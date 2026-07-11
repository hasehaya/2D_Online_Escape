namespace Escape.SceneObject.Wake
{
    public static class WakeLaserProgress
    {
        public static readonly FlagType[] TargetFlags =
        {
            FlagType.Wake_LaserTarget1,
            FlagType.Wake_LaserTarget2,
            FlagType.Wake_LaserTarget3
        };

        public const FlagType CompletedFlag = FlagType.Wake_LaserCompleted;
    }
}