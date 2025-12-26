namespace DLSS_Swapper.Data.BattleNet;

internal class BattleNetLauncherGame
{
    // Uid is generally the shorter code.
    // 
    // ProductCode is also called ProductID.
    // It is found in products.db as well as aggregate.json
    // It can be used in battlenet://game/{ProductCode} to open the game page.
    // Which just calls Battle.net.exe --uri="%1"
    //
    // LauncherId is usually an uppercase variant of Uid, and it is used to launch games with
    // Battle.net.exe --exec="launch {LauncherId}"
    // There is no known on disk reference to this value.

    public string Uid { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string LauncherId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public BattleNetLauncherGame(string uid, string productCode, string launcherId, string name)
    {
        Uid = uid;
        ProductCode = productCode;
        Name = name;
        LauncherId = launcherId;
    }
}
