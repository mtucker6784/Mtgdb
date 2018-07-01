using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Mtgdb.Gui
{
	public class HistorySubsystem
	{
		static HistorySubsystem()
		{
			_serializer = new JsonSerializer();
			_serializer.Converters.Add(new CustomConverter());
		}

		public HistorySubsystem(UndoConfig undoConfig)
		{
			_maxDepth = undoConfig.MaxDepth ?? 100;
		}

		public void LoadHistory(string historyFile)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(historyFile));

			if (File.Exists(historyFile))
			{
				HistoryState state;

				using (var fileReader = File.OpenText(historyFile))
				using (var jsonReader = new JsonTextReader(fileReader))
					state = _serializer.Deserialize<HistoryState>(jsonReader);

				_settingsHistory = state.SettingsHistory;
				_settingsIndex = state.SettingsIndex;
			}
			else
			{
				_settingsHistory = new List<GuiSettings>();
				var defaultSettings = new GuiSettings();
				Add(defaultSettings);
			}

			IsLoaded = true;
			Loaded?.Invoke();
		}



		public void Add(GuiSettings settings)
		{
			_settingsHistory.Add(settings);
			_settingsIndex = _settingsHistory.Count - 1;
		}

		public bool Undo()
		{
			validate();

			if (_settingsIndex > 0)
			{
				_settingsIndex--;
				return true;
			}

			return false;
		}

		public bool Redo()
		{
			validate();

			if (_settingsIndex < _settingsHistory.Count - 1)
			{
				_settingsIndex++;
				return true;
			}

			return false;
		}

		public void Save(string historyFile)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(historyFile));

			var state = getState();

			using (var writer = File.CreateText(historyFile))
			using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented, Indentation = 1, IndentChar = '\t' })
				_serializer.Serialize(jsonWriter, state);
		}

		private HistoryState getState()
		{
			int minSaveIndex = _settingsIndex - _maxDepth;
			int saveCount = 1 + 2 * _maxDepth;

			if (minSaveIndex < 0)
				minSaveIndex = 0;

			if (saveCount > _settingsHistory.Count - minSaveIndex)
				saveCount = _settingsHistory.Count - minSaveIndex;

			var state = new HistoryState
			{
				SettingsHistory = _settingsHistory.SkipFirst(minSaveIndex).TakeFirst(saveCount),
				SettingsIndex = _settingsIndex - minSaveIndex
			};

			return state;
		}

		private void validate()
		{
			if (_settingsIndex < 0 || _settingsIndex >= _settingsHistory.Count)
				throw new IndexOutOfRangeException();
		}



		public GuiSettings Current => _settingsHistory?[_settingsIndex];

		public bool CanRedo
		{
			get
			{
				validate();
				return _settingsIndex < _settingsHistory.Count - 1;
			}
		}

		public bool CanUndo
		{
			get
			{
				validate();
				return _settingsIndex > 0;
			}
		}

		public event Action Loaded;
		public bool IsLoaded { get; private set; }

		private int _settingsIndex;
		private List<GuiSettings> _settingsHistory;
		private readonly int _maxDepth;
		private static readonly JsonSerializer _serializer;
	}
}