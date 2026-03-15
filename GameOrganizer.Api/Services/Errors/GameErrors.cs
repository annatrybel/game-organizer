namespace GameOrganizer.Api.Services.Errors
{
    public static class GameErrors
    {
        private static readonly string objectName = "Game";

        public static ServiceError GameAlreadyInCollection() => new ServiceError(
            $"{objectName}.AlreadyInCollection", "Ta gra znajduje się już w Twojej kolekcji.");

        public static ServiceError GameAlreadyExists(string title) => new ServiceError(
            $"{objectName}.AlreadyExists", $"Gra o tytule '{title}' znajduje się już w bibliotece.");

        public static ServiceError ProposalAlreadyExists() => new ServiceError(
        $"{objectName}.ProposalAlreadyExists", "Ta gra została już zaproponowana i oczekuje na akceptację administratora.");
    }
}
