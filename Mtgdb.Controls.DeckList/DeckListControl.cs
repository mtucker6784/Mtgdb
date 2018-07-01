﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Mtgdb.Controls.Properties;
using Mtgdb.Dal;

namespace Mtgdb.Controls
{
	public partial class DeckListControl : UserControl
	{
		public event Action<object> Refreshed;
		public event Action<object> Scrolled;
		public event Action<object, Deck> DeckOpened;
		public event Action<object, Deck> DeckRenamed;
		public event Action<object> FilterByDeckModeChanged;

		public DeckListControl()
		{
			InitializeComponent();
		}

		public void Init(
			DeckListModel decks,
			IconRecognizer recognizer,
			DeckSearcher searcher,
			DeckDocumentAdapter adapter,
			Control tooltipOwner)
		{
			_deckListModel = decks;
			_tooltipOwner = tooltipOwner;

			_viewDeck.IconRecognizer = recognizer;
			_viewDeck.LayoutControlType = typeof(DeckListLayout);
			_viewDeck.DataSource = _filteredModels;

			_textBoxName.Visible = false;

			_customTooltip = new ViewDeckListTooltips(_tooltipOwner, _viewDeck);

			_searchSubsystem = new DeckSearchSubsystem(
				this,
				_textBoxSearch,
				_panelSearchIcon,
				_listBoxSuggest,
				searcher,
				_viewDeck);

			_menuFilterByDeckMode.SelectedIndex = 0;

			_viewDeck.MouseClicked += viewDeckClicked;
			_viewDeck.RowDataLoaded += viewDeckRowDataLoaded;
			_viewDeck.CardIndexChanged += viewScrolled;

			_textBoxName.LostFocus += nameLostFocus;
			_textBoxName.KeyDown += nameKeyDown;

			_menuFilterByDeckMode.SelectedIndexChanged += filterByDeckModeChanged;

			_searchSubsystem.TextApplied += applySearchResult;

			_searchSubsystem.SubscribeToEvents();
			_customTooltip.SubscribeToEvents();

			_higlightSubsystem = new SearchResultHiglightSubsystem(_viewDeck, _searchSubsystem, adapter);
			_higlightSubsystem.SubscribeToEvents();
		}



		public void SetUi(UiModel ui, TooltipController controller, DeckSuggestModel suggestModel)
		{
			_ui = ui;

			if (_searchSubsystem.SuggestModel != null)
				_searchSubsystem.UnsubscribeSuggestModelEvents();

			_searchSubsystem.SuggestModel = suggestModel;
			_searchSubsystem.Ui = ui;
			_searchSubsystem.SubscribeSuggestModelEvents();

			setupTooltips(controller);

			_searchSubsystem.UpdateSuggestInput();

			initModels();
		}

		public void StartThread() =>
			_searchSubsystem.StartThread();

		public void AbortThread() =>
			_searchSubsystem.AbortThread();

		private void initModels()
		{
			_model = new DeckModel(Deck.Create(), _ui) { Id = -1, IsCurrent = true };
			
			var models = _deckListModel.GetModels(_ui);

			foreach (var model in models)
			{
				model.Id = _models.Count;
				_models.Add(model);
			}

			_searchSubsystem.ModelChanged();
		}

		public void PriceLoaded()
		{
			foreach (var model in _models)
				model.DeckChanged();

			_searchSubsystem.ModelChanged();
		}

		public void DeckChanged(Deck deck)
		{
			_model.Deck = deck;
			_searchSubsystem.ModelChanged();
		}

		public void CollectionChanged()
		{
			foreach (var model in _models)
				model.CollectionChanged();

			_searchSubsystem.ModelChanged();
		}



		private void setupTooltips(TooltipController controller)
		{
			controller.SetTooltip(_tooltipOwner, "Deck name", "Type deck name.\r\n" +
				"press Enter to apply\r\n" +
				"press Esc to cancel",
				_textBoxName);

			controller.SetTooltip(_tooltipOwner,
				() => _searchSubsystem.SearchResult?.ParseErrorMessage != null
					? "Syntax error"
					: "Search decks",
				() => _searchSubsystem.SearchResult?.ParseErrorMessage ??
					"Type some query to narrow down the list of decks below\r\n" +
					"Example queries:\r\n" +
					"name: affin*\r\n" +
					"mana: \\{w\\} AND \\{u\\}\r\n\r\n" +
					"Ctrl+SPACE to get intellisense\r\n" +
					"Enter to apply\r\n" +
					"Ctrl+Backspace to delete one word\r\n" +
					"F1 to learn searh string syntax\r\n\r\n",
				_panelSearchIcon,
				_panelSearch,
				_textBoxSearch);

			string filterMode(FilterByDeckMode mode) =>
				_menuFilterByDeckMode.Items[(int) mode].ToString();

			controller.SetTooltip(_tooltipOwner,
				"Filter by deck mode",
				"Controls how search result of cards is affected by decks.\r\n\r\n" +
				$"- {filterMode(FilterByDeckMode.Ignored)}\r\n" +
				"    decks do not affect search result of cards\r\n" +
				$"- {filterMode(FilterByDeckMode.CurrentDeck)}\r\n" +
				"    show cards present in currently opened deck\r\n" +
				$"- {filterMode(FilterByDeckMode.FilteredSavedDecks)}\r\n" +
				"    show cards present in any saved deck from list below matching search criteria for " +
				"saved decks on the left",
				_menuFilterByDeckMode);

			controller.SetCustomTooltip(_customTooltip);
		}

		private void viewDeckClicked(object view, HitInfo hitInfo, MouseEventArgs mouseArgs)
		{
			if (hitInfo.CustomButtonIndex == DeckListLayout.CustomButtonAdd)
				saveCurrentDeck();
			else if (hitInfo.CustomButtonIndex == DeckListLayout.CustomButtonRemove)
				removeDeck((DeckModel) hitInfo.RowDataSource);
			else if (hitInfo.CustomButtonIndex == DeckListLayout.CustomButtonOpen)
				openDeck((DeckModel) hitInfo.RowDataSource);
			else if (hitInfo.CustomButtonIndex == DeckListLayout.CustomButtonRename)
				beginRenaming((DeckModel) hitInfo.RowDataSource, hitInfo.FieldBounds.Value);
		}

		private void viewDeckRowDataLoaded(object view, int rowHandle) =>
			endRenaming(commit: false);

		private void viewScrolled(object obj) =>
			Scrolled?.Invoke(this);

		private void applySearchResult()
		{
			var searchResult = _searchSubsystem?.SearchResult?.RelevanceById;

			_filteredModels.Clear();
			_filteredModels.Add(_model);

			var models = _models.Where(m => searchResult == null || searchResult.ContainsKey(m.Id));
			
			foreach (var model in models)
			{
				_filteredModels.Add(model);

				_cardIdsInFilteredDecks.UnionWith(model.Deck.MainDeck.Order);
				_cardIdsInFilteredDecks.UnionWith(model.Deck.Sideboard.Order);
			}
			
			_viewDeck.RefreshData();
			Refreshed?.Invoke(this);
		}

		private void filterByDeckModeChanged(object sender, EventArgs e)
		{
			FilterByDeckMode = (FilterByDeckMode) _menuFilterByDeckMode.SelectedIndex;
			FilterByDeckModeChanged?.Invoke(this);
		}


		private void saveCurrentDeck()
		{
			var copy = _model.Deck.Copy();
			var copyModel = new DeckModel(copy, _ui) { Id = _models.Count };

			_deckListModel.Insert(0, copy);
			_models.Insert(0, copyModel);

			_searchSubsystem.ModelChanged();
		}

		private void removeDeck(DeckModel deckModel)
		{
			_models.Remove(deckModel);
			_deckListModel.Remove(deckModel.Deck);

			_searchSubsystem.ModelChanged();
		}

		private void openDeck(DeckModel deckModel) =>
			DeckOpened?.Invoke(this, deckModel.Deck);



		private void nameLostFocus(object sender, EventArgs e) =>
			endRenaming(commit: true);

		private void nameKeyDown(object sender, KeyEventArgs e)
		{
			bool handled = true;

			if (e.KeyCode == Keys.Enter)
				endRenaming(commit: true);
			else if (e.KeyCode == Keys.Escape)
				endRenaming(commit: false);
			else
				handled = false;

			if (handled)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private void beginRenaming(DeckModel model, Rectangle fieldBounds)
		{
			_renamedModel = model;

			fieldBounds.Offset(_viewDeck.Location);
			_textBoxName.Bounds = fieldBounds;
			_textBoxName.Text = model.Deck.Name;
			_textBoxName.SelectAll();

			_textBoxName.Visible = true;
			_textBoxName.Focus();
		}

		private void endRenaming(bool commit)
		{
			if (_renamedModel == null)
				return;

			var renamedModel = _renamedModel;

			if (commit)
				_deckListModel.Rename(renamedModel.Deck, _textBoxName.Text);

			_renamedModel = null;
			_textBoxName.Visible = false;
			_textBoxName.Text = string.Empty;

			if (commit)
			{
				_searchSubsystem.ModelChanged();

				if (renamedModel.IsCurrent)
					DeckRenamed?.Invoke(this, renamedModel.Deck);
			}
		}



		public void Scale()
		{
			scalePanelIcon(_panelSearchIcon);

			_panelSearch.Height = _panelSearch.Height.ByDpiHeight();
			_menuFilterByDeckMode.ScaleDpi();

			scaleLayoutView(_viewDeck);
		}

		private static void scalePanelIcon(BorderedPanel panel)
		{
			panel.ScaleDpi();
			panel.BackgroundImage = ((Bitmap) panel.BackgroundImage).HalfResizeDpi();
		}

		private static void scaleLayoutView(LayoutViewControl view)
		{
			var sortIcon = view.SortOptions.Icon;

			view.SortOptions.Icon = sortIcon?.HalfResizeDpi();
			view.SortOptions.AscIcon = view.SortOptions.AscIcon?.HalfResizeDpi();
			view.SortOptions.DescIcon = view.SortOptions.DescIcon?.HalfResizeDpi();
			view.SearchOptions.Button.Icon = view.SearchOptions.Button.Icon?.HalfResizeDpi();

			view.LayoutOptions.AlignTopLeftIcon = view.LayoutOptions.AlignTopLeftIcon?.HalfResizeDpi();
			view.LayoutOptions.AlignTopLeftHoveredIcon = view.LayoutOptions.AlignTopLeftHoveredIcon?.HalfResizeDpi();

			view.ProbeCardCreating += probeCardCreating;
		}

		private static void probeCardCreating(object view, LayoutControl probeCard)
		{
			var card = (DeckListLayout) probeCard;

			card.ScaleDpi();

			foreach (var field in card.Fields)
			{
				field.Image = ((Bitmap) field.Image)?.HalfResizeDpi();

				for (int i = 0; i < field.CustomButtons.Count; i++)
				{
					var button = field.CustomButtons[i];
					button.Icon = button.Icon?.ResizeDpi();
				}
			}

			card.ImageDeckBox = Resources.deckbox.ResizeDpi();
			card.ImageDeckBoxOpened = Resources.deckbox_opened.ResizeDpi();
		}

		public bool AnyFilteredDeckContains(Card c) =>
			_cardIdsInFilteredDecks.Contains(c.Id);



		public bool HideScroll
		{
			get => _viewDeck.LayoutOptions.HideScroll;
			set => _viewDeck.LayoutOptions.HideScroll = value;
		}

		public bool AllowPartialCard
		{
			get => _viewDeck.LayoutOptions.AllowPartialCards;
			set => _viewDeck.LayoutOptions.AllowPartialCards = value;
		}

		public FilterByDeckMode FilterByDeckMode
		{
			get => _filterByDeckMode;
			set
			{
				if (_filterByDeckMode == value)
					return;

				_filterByDeckMode = value;
				_menuFilterByDeckMode.SelectedIndex = (int) value;
			}
		}

		public LayoutViewControl DeckListView => _viewDeck;
		public int ScrollPosition => _viewDeck.CardIndex;
		public int MaxScroll => _viewDeck.Count;
		public int FilteredDecksCount => _viewDeck.Count - 1;

		private DeckModel _model;

		private object _tooltipOwner;

		private UiModel _ui;
		private DeckListModel _deckListModel;

		private DeckModel _renamedModel;
		private ViewDeckListTooltips _customTooltip;
		private DeckSearchSubsystem _searchSubsystem;

		private FilterByDeckMode _filterByDeckMode;
		private SearchResultHiglightSubsystem _higlightSubsystem;

		private readonly HashSet<string> _cardIdsInFilteredDecks = new HashSet<string>(Str.Comparer);
		private readonly List<DeckModel> _models = new List<DeckModel>();
		private readonly List<DeckModel> _filteredModels = new List<DeckModel>();
	}
}
