﻿using LineLiveSitePlugin;
using MildomSitePlugin;
using MirrativSitePlugin;
using MixerSitePlugin;
using NicoSitePlugin;
using OpenrecSitePlugin;
using PeriscopeSitePlugin;
using Plugin;
using PluginCommon;
using SitePlugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using TwicasSitePlugin;
using TwitchSitePlugin;
using WhowatchSitePlugin;
using YouTubeLiveSitePlugin;

namespace VoiceRoidPlugin
{
    static class MessageParts
    {
        public static string ToTextWithImageAlt(this IEnumerable<IMessagePart> parts)
        {
            string s = "";
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part is IMessageText text)
                    {
                        s += text;
                    }
                    else if (part is IMessageImage image)
                    {
                        s += image.Alt;
                    }
                }
            }
            return s;
        }
    }
    [Export(typeof(IPlugin))]
    public class VoiceRoidPlugin : IPlugin, IDisposable
    {
        private Options _options;
        Process _voiceRoidProcess;
        private VoiceRoidController _voiceRoidController;
        public string Name => "VoiceRoid連携";

        public string Description => "ゆかりんに読んでもらうプラグインです";
        public void OnTopmostChanged(bool isTopmost)
        {
            if (_settingsView != null)
            {
                _settingsView.Topmost = isTopmost;
            }
        }
        public void OnLoaded()
        {
            try
            {
                var s = Host.LoadOptions(GetSettingsFilePath());
                _options.Deserialize(s);
            }
            catch (System.IO.FileNotFoundException) { }
            try
            {
                if (_options.IsExecVoiceRoidAtBoot && !IsExecutingProcess("VOICEROID"))
                {
                    StartVoiceRoid();
                }
                _voiceRoidController = new VoiceRoidController(_options);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// 指定したプロセス名を持つプロセスが起動中か
        /// </summary>
        /// <param name="processName">プロセス名</param>
        /// <returns></returns>
        private bool IsExecutingProcess(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public void OnClosing()
        {
            _settingsView?.ForceClose();
            var s = _options.Serialize();
            Host.SaveOptions(GetSettingsFilePath(), s);
            if (_voiceRoidProcess != null && _options.IsKillVoiceRoid)
            {
                try
                {
                    _voiceRoidProcess.Kill();
                }
                catch (Exception) { }
            }
        }
        private void StartVoiceRoid()
        {
            if (_voiceRoidProcess == null && System.IO.File.Exists(_options.VoiceRoidPath))
            {
                _voiceRoidProcess = Process.Start(_options.VoiceRoidPath);
                _voiceRoidProcess.EnableRaisingEvents = true;
                _voiceRoidProcess.Exited += VoiceRoidProcessExited;
            }
        }

        private void VoiceRoidProcessExited(object sender, EventArgs e)
        {
            try
            {
                _voiceRoidProcess?.Close();//2018/03/25ここで_bouyomiChanProcessがnullになる場合があった
            }
            catch { }
            _voiceRoidProcess = null;
        }

        private static (string name, string comment) GetData(ISiteMessage message, Options options)
        {
            string name = null;
            string comment = null;
            if (false) { }
            else if (message is IYouTubeLiveMessage youTubeLiveMessage)
            {
                switch (youTubeLiveMessage.YouTubeLiveMessageType)
                {
                    case YouTubeLiveMessageType.Connected:
                        if (options.IsYouTubeLiveConnect)
                        {
                            name = null;
                            comment = (youTubeLiveMessage as IYouTubeLiveConnected).Text;
                        }
                        break;
                    case YouTubeLiveMessageType.Disconnected:
                        if (options.IsYouTubeLiveDisconnect)
                        {
                            name = null;
                            comment = (youTubeLiveMessage as IYouTubeLiveDisconnected).Text;
                        }
                        break;
                    case YouTubeLiveMessageType.Comment:
                        if (options.IsYouTubeLiveComment)
                        {
                            if (options.IsYouTubeLiveCommentNickname)
                            {
                                name = (youTubeLiveMessage as IYouTubeLiveComment).NameItems.ToText();
                            }
                            if (options.IsYouTubeLiveCommentStamp)
                            {
                                comment = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToTextWithImageAlt();
                            }
                            else
                            {
                                comment = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToText();
                            }
                        }
                        break;
                    case YouTubeLiveMessageType.Superchat:
                        if (options.IsYouTubeLiveSuperchat)
                        {
                            if (options.IsYouTubeLiveSuperchatNickname)
                            {
                                name = (youTubeLiveMessage as IYouTubeLiveSuperchat).NameItems.ToText();
                            }
                            //TODO:superchat中のスタンプも読ませるべきでは？
                            comment = (youTubeLiveMessage as IYouTubeLiveSuperchat).CommentItems.ToText();
                        }
                        break;
                }
            }
            else if (message is IOpenrecMessage openrecMessage)
            {
                switch (openrecMessage.OpenrecMessageType)
                {
                    case OpenrecMessageType.Connected:
                        if (options.IsOpenrecConnect)
                        {
                            name = null;
                            comment = (openrecMessage as IOpenrecConnected).Text;
                        }
                        break;
                    case OpenrecMessageType.Disconnected:
                        if (options.IsOpenrecDisconnect)
                        {
                            name = null;
                            comment = (openrecMessage as IOpenrecDisconnected).Text;
                        }
                        break;
                    case OpenrecMessageType.Comment:
                        if (options.IsOpenrecComment)
                        {
                            if (options.IsOpenrecCommentNickname)
                            {
                                name = (openrecMessage as IOpenrecComment).NameItems.ToText();
                            }
                            comment = (openrecMessage as IOpenrecComment).MessageItems.ToText();
                        }
                        break;
                }
            }
            else if (message is ITwitchMessage twitchMessage)
            {
                switch (twitchMessage.TwitchMessageType)
                {
                    case TwitchMessageType.Connected:
                        if (options.IsTwitchConnect)
                        {
                            name = null;
                            comment = (twitchMessage as ITwitchConnected).Text;
                        }
                        break;
                    case TwitchMessageType.Disconnected:
                        if (options.IsTwitchDisconnect)
                        {
                            name = null;
                            comment = (twitchMessage as ITwitchDisconnected).Text;
                        }
                        break;
                    case TwitchMessageType.Comment:
                        if (options.IsTwitchComment)
                        {
                            if (options.IsTwitchCommentNickname)
                            {
                                name = (twitchMessage as ITwitchComment).DisplayName;
                            }
                            comment = (twitchMessage as ITwitchComment).CommentItems.ToText();
                        }
                        break;
                }
            }
            else if (message is INicoMessage NicoMessage)
            {
                switch (NicoMessage.NicoMessageType)
                {
                    case NicoMessageType.Connected:
                        if (options.IsNicoConnect)
                        {
                            name = null;
                            comment = (NicoMessage as INicoConnected).Text;
                        }
                        break;
                    case NicoMessageType.Disconnected:
                        if (options.IsNicoDisconnect)
                        {
                            name = null;
                            comment = (NicoMessage as INicoDisconnected).Text;
                        }
                        break;
                    case NicoMessageType.Comment:
                        if (options.IsNicoComment)
                        {
                            if (options.IsNicoCommentNickname)
                            {
                                name = (NicoMessage as INicoComment).UserName;
                            }
                            comment = (NicoMessage as INicoComment).Text;
                        }
                        break;
                    case NicoMessageType.Item:
                        if (options.IsNicoItem)
                        {
                            if (options.IsNicoItemNickname)
                            {
                                //name = (NicoMessage as INicoItem).NameItems.ToText();
                            }
                            comment = (NicoMessage as INicoItem).Text;
                        }
                        break;
                    case NicoMessageType.Ad:
                        if (options.IsNicoAd)
                        {
                            name = null;
                            comment = (NicoMessage as INicoAd).Text;
                        }
                        break;
                }
            }
            else if (message is ITwicasMessage twicasMessage)
            {
                switch (twicasMessage.TwicasMessageType)
                {
                    case TwicasMessageType.Connected:
                        if (options.IsTwicasConnect)
                        {
                            name = null;
                            comment = (twicasMessage as ITwicasConnected).Text;
                        }
                        break;
                    case TwicasMessageType.Disconnected:
                        if (options.IsTwicasDisconnect)
                        {
                            name = null;
                            comment = (twicasMessage as ITwicasDisconnected).Text;
                        }
                        break;
                    case TwicasMessageType.Comment:
                        if (options.IsTwicasComment)
                        {
                            if (options.IsTwicasCommentNickname)
                            {
                                name = (twicasMessage as ITwicasComment).UserName;
                            }
                            comment = (twicasMessage as ITwicasComment).CommentItems.ToText();
                        }
                        break;
                }
            }
            else if (message is ILineLiveMessage lineLiveMessage)
            {
                switch (lineLiveMessage.LineLiveMessageType)
                {
                    case LineLiveMessageType.Connected:
                        if (options.IsLineLiveConnect)
                        {
                            name = null;
                            comment = (lineLiveMessage as ILineLiveConnected).Text;
                        }
                        break;
                    case LineLiveMessageType.Disconnected:
                        if (options.IsLineLiveDisconnect)
                        {
                            name = null;
                            comment = (lineLiveMessage as ILineLiveDisconnected).Text;
                        }
                        break;
                    case LineLiveMessageType.Comment:
                        if (options.IsLineLiveComment)
                        {
                            if (options.IsLineLiveCommentNickname)
                            {
                                name = (lineLiveMessage as ILineLiveComment).DisplayName;
                            }
                            comment = (lineLiveMessage as ILineLiveComment).Text;
                        }
                        break;
                }
            }
            else if (message is IWhowatchMessage whowatchMessage)
            {
                switch (whowatchMessage.WhowatchMessageType)
                {
                    case WhowatchMessageType.Connected:
                        if (options.IsWhowatchConnect)
                        {
                            name = null;
                            comment = (whowatchMessage as IWhowatchConnected).Text;
                        }
                        break;
                    case WhowatchMessageType.Disconnected:
                        if (options.IsWhowatchDisconnect)
                        {
                            name = null;
                            comment = (whowatchMessage as IWhowatchDisconnected).Text;
                        }
                        break;
                    case WhowatchMessageType.Comment:
                        if (options.IsWhowatchComment)
                        {
                            if (options.IsWhowatchCommentNickname)
                            {
                                name = (whowatchMessage as IWhowatchComment).UserName;
                            }
                            comment = (whowatchMessage as IWhowatchComment).Comment;
                        }
                        break;
                    case WhowatchMessageType.Item:
                        if (options.IsWhowatchItem)
                        {
                            if (options.IsWhowatchItemNickname)
                            {
                                name = (whowatchMessage as IWhowatchItem).UserName;
                            }
                            comment = (whowatchMessage as IWhowatchItem).Comment;
                        }
                        break;
                }
            }
            else if (message is IMirrativMessage mirrativMessage)
            {
                switch (mirrativMessage.MirrativMessageType)
                {
                    case MirrativMessageType.Connected:
                        if (options.IsMirrativConnect)
                        {
                            name = null;
                            comment = (mirrativMessage as IMirrativConnected).Text;
                        }
                        break;
                    case MirrativMessageType.Disconnected:
                        if (options.IsMirrativDisconnect)
                        {
                            name = null;
                            comment = (mirrativMessage as IMirrativDisconnected).Text;
                        }
                        break;
                    case MirrativMessageType.Comment:
                        if (options.IsMirrativComment)
                        {
                            if (options.IsMirrativCommentNickname)
                            {
                                name = (mirrativMessage as IMirrativComment).UserName;
                            }
                            comment = (mirrativMessage as IMirrativComment).Text;
                        }
                        break;
                    case MirrativMessageType.JoinRoom:
                        if (options.IsMirrativJoinRoom)
                        {
                            name = null;
                            comment = (mirrativMessage as IMirrativJoinRoom).Text;
                        }
                        break;
                    case MirrativMessageType.Item:
                        if (options.IsMirrativItem)
                        {
                            name = null;
                            comment = (mirrativMessage as IMirrativItem).Text;
                        }
                        break;
                }
            }
            else if (message is IPeriscopeMessage PeriscopeMessage)
            {
                switch (PeriscopeMessage.PeriscopeMessageType)
                {
                    case PeriscopeMessageType.Connected:
                        if (options.IsPeriscopeConnect)
                        {
                            name = null;
                            comment = (PeriscopeMessage as IPeriscopeConnected).Text;
                        }
                        break;
                    case PeriscopeMessageType.Disconnected:
                        if (options.IsPeriscopeDisconnect)
                        {
                            name = null;
                            comment = (PeriscopeMessage as IPeriscopeDisconnected).Text;
                        }
                        break;
                    case PeriscopeMessageType.Comment:
                        if (options.IsPeriscopeComment)
                        {
                            if (options.IsPeriscopeCommentNickname)
                            {
                                name = (PeriscopeMessage as IPeriscopeComment).DisplayName;
                            }
                            comment = (PeriscopeMessage as IPeriscopeComment).Text;
                        }
                        break;
                    case PeriscopeMessageType.Join:
                        if (options.IsPeriscopeJoin)
                        {
                            name = null;
                            comment = (PeriscopeMessage as IPeriscopeJoin).Text;
                        }
                        break;
                    case PeriscopeMessageType.Leave:
                        if (options.IsPeriscopeLeave)
                        {
                            name = null;
                            comment = (PeriscopeMessage as IPeriscopeLeave).Text;
                        }
                        break;
                }
            }
            else if (message is IMixerMessage MixerMessage)
            {
                switch (MixerMessage.MixerMessageType)
                {
                    case MixerMessageType.Connected:
                        if (options.IsMixerConnect)
                        {
                            name = null;
                            comment = (MixerMessage as IMixerConnected).Text;
                        }
                        break;
                    case MixerMessageType.Disconnected:
                        if (options.IsMixerDisconnect)
                        {
                            name = null;
                            comment = (MixerMessage as IMixerDisconnected).Text;
                        }
                        break;
                    case MixerMessageType.Comment:
                        if (options.IsMixerComment)
                        {
                            if (options.IsMixerCommentNickname)
                            {
                                name = (MixerMessage as IMixerComment).UserName;
                            }
                            comment = (MixerMessage as IMixerComment).CommentItems.ToText();
                        }
                        break;
                        //case MixerMessageType.Join:
                        //    if (_options.IsMixerJoin)
                        //    {
                        //        name = null;
                        //        comment = (MixerMessage as IMixerJoin).CommentItems.ToText();
                        //    }
                        //    break;
                        //case MixerMessageType.Leave:
                        //    if (_options.IsMixerLeave)
                        //    {
                        //        name = null;
                        //        comment = (MixerMessage as IMixerLeave).CommentItems.ToText();
                        //    }
                        //    break;
                }
            }
            else if (message is IMildomMessage MildomMessage)
            {
                switch (MildomMessage.MildomMessageType)
                {
                    case MildomMessageType.Connected:
                        if (options.IsMildomConnect)
                        {
                            name = null;
                            comment = (MildomMessage as IMildomConnected).Text;
                        }
                        break;
                    case MildomMessageType.Disconnected:
                        if (options.IsMildomDisconnect)
                        {
                            name = null;
                            comment = (MildomMessage as IMildomDisconnected).Text;
                        }
                        break;
                    case MildomMessageType.Comment:
                        if (options.IsMildomComment)
                        {
                            if (options.IsMildomCommentNickname)
                            {
                                name = (MildomMessage as IMildomComment).UserName;
                            }
                            comment = (MildomMessage as IMildomComment).CommentItems.ToText();
                        }
                        break;
                    case MildomMessageType.JoinRoom:
                        if (options.IsMildomJoin)
                        {
                            name = null;
                            comment = (MildomMessage as IMildomJoinRoom).CommentItems.ToText();
                        }
                        break;
                        //case MildomMessageType.Leave:
                        //    if (_options.IsMildomLeave)
                        //    {
                        //        name = null;
                        //        comment = (MildomMessage as IMildomLeave).CommentItems.ToText();
                        //    }
                        //    break;
                }
            }
            return (name, comment);
        }
        public void OnMessageReceived(ISiteMessage message, IMessageMetadata messageMetadata)
        {
            if (!_options.IsEnabled || messageMetadata.IsNgUser || messageMetadata.IsInitialComment || (messageMetadata.Is184 && !_options.Want184Read))
                return;

            var (name, comment) = GetData(message, _options);

            //nameがnullでは無い場合かつUser.Nicknameがある場合はNicknameを採用
            if (!string.IsNullOrEmpty(name) && messageMetadata.User != null && !string.IsNullOrEmpty(messageMetadata.User.Nickname))
            {
                name = messageMetadata.User.Nickname;
            }
            try
            {
                // オプションで指定するFormatの変数名をString.Formatの引数に変換して入力変換を行う。
                var formatCode = _options.FormatCode;
                formatCode = formatCode.Replace("$name", "{0}");
                formatCode = formatCode.Replace("$comment", "{1}");
                var dataToRead = string.Format(formatCode, name, comment);
                TalkText(dataToRead);
            }
            catch (System.Runtime.Remoting.RemotingException)
            {
                //多分棒読みちゃんが起動していない。
                if (_voiceRoidProcess == null && System.IO.File.Exists(_options.VoiceRoidPath))
                {
                    _voiceRoidProcess = Process.Start(_options.VoiceRoidPath);
                    _voiceRoidProcess.EnableRaisingEvents = true;
                    _voiceRoidProcess.Exited += (s, e) =>
                    {
                        try
                        {
                            _voiceRoidProcess?.Close();//2018/03/25ここで_bouyomiChanProcessがnullになる場合があった
                        }
                        catch { }
                        _voiceRoidProcess = null;
                    };
                }
                //起動するまでの間にコメントが投稿されたらここに来てしまうが諦める。
            }
            catch (Exception)
            {

            }
        }

        private int TalkText(string text)
        {
            if (!_voiceRoidController.IsEnable()) throw new Exception("ボイロがないです");

            _voiceRoidController.TalkMessageNow(text);
            return -1;
        }

        public IPluginHost Host { get; set; }
        public string GetSettingsFilePath()
        {
            //ここでRemotingExceptionが発生。終了時の処理だが、既にHostがDisposeされてるのかも。
            var dir = Host.SettingsDirPath;
            return System.IO.Path.Combine(dir, $"{Name}.xml");
        }
        ConfigView _settingsView;
        public void ShowSettingView()
        {
            if (_settingsView == null)
            {
                _settingsView = new ConfigView
                {
                    DataContext = new ConfigViewModel(_options)
                };
            }
            _settingsView.Topmost = Host.IsTopmost;
            _settingsView.Left = Host.MainViewLeft;
            _settingsView.Top = Host.MainViewTop;

            _settingsView.Show();
        }
        public VoiceRoidPlugin()
        {
            _options = new Options();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }
                if (_voiceRoidProcess != null)
                {
                    _voiceRoidProcess.Close();
                    _voiceRoidProcess = null;
                }
                _disposedValue = true;
            }
        }

        ~VoiceRoidPlugin()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    class OptionsLoader
    {
        public Options Load(string path)
        {
            var options = new Options();
            return options;
        }
        public void Save(Options options, string path)
        {

        }
    }
}
