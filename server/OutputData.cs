namespace Cerrio.Samples.SDC
{
    public class OutputData
    {
        public string Key { get; set; }

        public string TwitterHandle { get; set; }

        public string RealName { get; set; }

        public string ProfilePic { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public string GroupName { get; set; }

        public double GroupCenterX { get; set; }

        public double GroupCenterY { get; set; }

        public string OriginatingUser { get; set; }

        public void UpdateKey()
        {
            Key = OriginatingUser + "|" + TwitterHandle;
        }
    }
}
