namespace BonfireSilksong
{
    public static class CursorPatchHelpers
    {
        /// <summary>
        /// ListenForX classes e.g. ListenForAttack, which listen to user input:
        /// Helper for disabling certain inputs at benches to allow GUI button clicking with custom cursor.
        /// </summary>
        public static bool AllowInput()
        {
            var input = GameManager.instance?.inputHandler;
            if (input == null || !input.gameController.IsAttached) return true;

            if (PlayerData.instance.atBench) return false;

            return true;
        }
    }
}