using Lucene.Net.Search;

namespace Mtgdb.Index
{
	public class EmptyPhraseQuery : Query
	{
		public EmptyPhraseQuery(string field)
		{
			Field = field;
		}

		public string Field { get; }

		public override string ToString(string field) => field + ": \"\"";
	}
}