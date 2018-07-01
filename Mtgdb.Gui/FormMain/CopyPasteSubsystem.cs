using System.Linq;
using System.Windows.Forms;
using Mtgdb.Controls;
using Mtgdb.Dal;

namespace Mtgdb.Gui
{
	public class CopyPasteSubsystem
	{
		public CopyPasteSubsystem(
			CardRepository cardRepo,
			DeckSerializationSubsystem serialization,
			CollectionEditorModel collection,
			DeckEditorModel deckEditor,
			IFormRoot formRoot,
			FormMain targetForm,
			params Control[] targetControls)
		{
			_cardRepo = cardRepo;
			_serialization = serialization;
			_collection = collection;
			_deckEditor = deckEditor;
			_formRoot = formRoot;
			_targetForm = targetForm;
			_targetControls = targetControls;
		}

		public void SubscribeToEvents()
		{
			foreach (var control in _targetControls)
			{
				control.AllowDrop = true;
				control.DragEnter += deckDragEnter;
				control.DragDrop += deckDragDropped;
			}
		}

		public void UnsubscribeFromEvents()
		{
			foreach (var control in _targetControls)
			{
				control.DragEnter -= deckDragEnter;
				control.DragDrop -= deckDragDropped;
			}
		}

		private static void deckDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetFormats().Contains(DataFormats.Text))
				e.Effect = DragDropEffects.Copy;
		}

		private void deckDragDropped(object sender, DragEventArgs e)
		{
			if (_cardRepo.IsLoadingComplete)
			{
				dragDropped(e.Data);
				return;
			}

			_cardRepo.LoadingComplete += () => { _targetForm.Invoke(delegate { dragDropped(e.Data); }); };

			MessageBox.Show(_targetForm,
				"Mtgdb.Gui is loading cards.\r\n" +
				"When completed, the deck(s) will be opened.",
				"Opening deck(s) delayed",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private void dragDropped(IDataObject dragData)
		{
			if (dragData.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[]) dragData.GetData(DataFormats.FileDrop);

				var decks = files.Select(f => _serialization.LoadFile(f))
					.ToArray();

				var failedDecks = decks.Where(d => d.Error != null).ToArray();
				var loadedDecks = decks.Where(d => d.Error == null).ToArray();

				if (failedDecks.Length > 0)
				{
					var message = string.Join(Str.Endl,
						failedDecks.Select(f => $"{f.File}{Str.Endl}{f.Error}{Str.Endl}"));

					MessageBox.Show(message);
				}

				if (loadedDecks.Length > 0)
					_targetForm.LoadDeck(loadedDecks[0]);

				for (int i = 1; i < loadedDecks.Length; i++)
				{
					var deck = loadedDecks[i];
					_formRoot.AddTab(form => ((FormMain) form).ScheduleOpeningDeck(deck));
				}
			}
			else if (dragData.GetFormats().Contains(DataFormats.Text))
			{
				string text = (string) dragData.GetData(DataFormats.Text, autoConvert: true);

				if (Control.ModifierKeys == Keys.Alt)
					pasteCollectionFromText(text, append: false);
				else if (Control.ModifierKeys == (Keys.Alt | Keys.Shift))
					pasteCollectionFromText(text, append: true);
				else if (Control.ModifierKeys == Keys.None)
					pasteDeckFromText(text, append: false);
				else if (Control.ModifierKeys == Keys.Shift)
					pasteDeckFromText(text, append: true);
			}
		}

		private void pasteCollectionFromText(string text, bool append)
		{
			var deck = _serialization.LoadSerialized("*.txt", text);

			if (deck.Error != null)
				MessageBox.Show(deck.Error);
			else
				_collection.LoadCollection(deck, append);
		}

		private void pasteDeckFromText(string text, bool append)
		{
			var deck = _serialization.LoadSerialized("*.txt", text);

			if (deck.Error != null)
				MessageBox.Show(deck.Error);
			else
				_deckEditor.Paste(deck, append, _cardRepo);
		}

		public void PasteDeck(bool append)
		{
			if (!_cardRepo.IsLoadingComplete)
				return;

			var text = Clipboard.GetText();
			if (string.IsNullOrWhiteSpace(text))
				return;

			pasteDeckFromText(text, append);
		}

		public void PasteCollection(bool append)
		{
			if (!_cardRepo.IsLoadingComplete)
				return;

			var text = Clipboard.GetText();
			if (string.IsNullOrWhiteSpace(text))
				return;

			pasteCollectionFromText(text, append);
		}

		public void CopyCollection()
		{
			var deck = Deck.Create(
				_collection.CountById?.ToDictionary(),
				_collection.CountById?.Keys.OrderBy(_ => _cardRepo.CardsById[_].NameEn).ToList(),
				null,
				null);

			var serialized = _serialization.SaveSerialized("*.txt", deck);
			Clipboard.SetText(serialized);
		}

		public void CopyDeck()
		{
			Deck deck;

			switch (_targetForm.DeckZone)
			{
				case Zone.SampleHand:
					deck = copyMainDeck();
					break;
				case Zone.Side:
					deck = copySideDeck();
					break;
				case Zone.Main:
					deck = GetDeckCopy();
					break;
				default:
					return;
			}

			var serialized = _serialization.SaveSerialized("*.txt", deck);
			Clipboard.SetText(serialized);
		}

		private Deck copyMainDeck() =>
			Deck.Create(
				_deckEditor.SampleHand.CountById.ToDictionary(),
				_deckEditor.SampleHand.CardsIds.ToList(),
				null,
				null);

		private Deck copySideDeck() =>
			Deck.Create(
				_deckEditor.SideDeck.CountById.ToDictionary(),
				_deckEditor.SideDeck.CardsIds.ToList(),
				null,
				null);

		public Deck GetDeckCopy()
		{
			var result = Deck.Create(
				_deckEditor.MainDeck.CountById.ToDictionary(),
				_deckEditor.MainDeck.CardsIds.ToList(),
				_deckEditor.SideDeck.CountById.ToDictionary(),
				_deckEditor.SideDeck.CardsIds.ToList());

			result.Name = _deckEditor.DeckName;
			result.File = _deckEditor.DeckFile;

			return result;
		}

		public void LoadDeck(Deck deck)
		{
			_deckEditor.DeckFile = deck.File;
			_deckEditor.DeckName = deck.Name;

			_deckEditor.SetDeck(deck, _cardRepo);
			_deckEditor.Shuffle();
		}

		private readonly CardRepository _cardRepo;
		private readonly DeckSerializationSubsystem _serialization;
		private readonly CollectionEditorModel _collection;
		private readonly DeckEditorModel _deckEditor;
		private readonly IFormRoot _formRoot;
		private readonly FormMain _targetForm;
		private readonly Control[] _targetControls;
	}
}