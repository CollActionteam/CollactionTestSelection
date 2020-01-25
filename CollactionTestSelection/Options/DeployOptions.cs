namespace CollactionTestSelection.Options
{
    public sealed class DeployOptions
    {
        public string AWS_ACCESS_KEY_ID { get; set; }

        public string AWS_SECRET_ACCESS_KEY { get; set; }

        public string AWS_DEFAULT_REGION { get; set; }

        public string AWS_CLUSTER { get; set; }

        public string AWS_SERVICE { get; set; }

        public string DOCKER_IMAGE { get; set; }

        public int TIMEOUT { get; set; } = 600;

        public int DESIRED_COUNT { get; set; } = 1;

        public int MAX_COUNT { get; set; } = 2;
    }
}
