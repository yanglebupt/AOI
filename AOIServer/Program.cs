using System;
using System.Threading;
using System.Threading.Tasks;
using YLCommon;

Logger.cfg.enableSave = false;
Logger.cfg.showTrace = false;
Logger.EnableSetting();

NetworkConfig.logger.warn = Logger.Warn;
NetworkConfig.logger.error = Logger.Error;
NetworkConfig.logger.info = Logger.Info;

int monsterCount = 0;

Task _ = Task.Run(() =>
{
    GameGlobal.Instance.Init();
    while (true)
    {
        for (int i = 0; i < monsterCount; i++)
            GameGlobal.Instance.CreateServerEntity();

        monsterCount = 0;
        GameGlobal.Instance.Tick();
        Thread.Sleep(10);
    }
});

while (true)
{
    string? ipt = Console.ReadLine();
    if(ipt != null)
        monsterCount = int.Parse(ipt);
}