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
    }
}
