namespace GameOrganizer.Api.Services.Errors
{
    public static class GameErrors
    {
        private static readonly string objectName = "Game";

        public static ServiceError GameAlreadyInCollection() => new ServiceError(
            $"{objectName}.AlreadyInCollection", "Ta gra znajduje się już w Twojej kolekcji.");
    }
}
