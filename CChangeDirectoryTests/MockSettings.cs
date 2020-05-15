namespace CChangeDirectory.Tests
{
    internal class MockSettings : ISettings
    {
        int dataSet;

        public MockSettings(int testDataSet)
        {
            this.dataSet = testDataSet;
        }

        public string[] GetList(string settingName)
        {
            if (dataSet == 1)
            {
                return new string[] { };
            }
            else if (dataSet == 2)
            {
                if (settingName == "include")
                {
                    return new[] { @"duck\goose\farm" };
                }
            }
            return new string[] { };
        }
    }
}