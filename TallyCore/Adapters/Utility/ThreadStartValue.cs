namespace NetTally.Adapters
{
    public class ThreadStartValue
    {
        public bool ByNumber { get; }
        public int Number { get; }
        public int Page { get; }
        public int ID { get; }

        public ThreadStartValue(bool byNumber, int number = 0, int page = 0, int id = 0)
        {
            ByNumber = byNumber;
            Number = number;
            Page = page;
            ID = id;
        }

        /// <summary>
        /// Get the starting page for tallying for the provided quest, using either
        /// the explicitly determined page number, or the calculated page number 
        /// based on the starting post and the quest's post per page.
        /// </summary>
        /// <param name="quest">The quest this info is for.</param>
        /// <returns>Returns the page number to start tallying.</returns>
        public int GetStartPage(IQuest quest)
        {
            if (ByNumber)
            {
                return quest.GetPageNumberOf(Number);
            }

            return Page;
        }
    }
}
