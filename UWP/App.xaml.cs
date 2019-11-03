using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Flurl.Http;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using WeiPo.Common;
using WeiPo.Services;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Newtonsoft.Json.Linq;

namespace WeiPo
{
    public sealed partial class App
    {
        public App()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            FlurlHttp.Configure(settings =>
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    Converters =
                    {
                        new JsonNumberConverter()
                    },
                    Error = (sender, args) =>
                    {
                        var currentError = args.ErrorContext.Error.Message;
                        Debug.WriteLine(currentError);
                        args.ErrorContext.Handled = true;
                    },
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                };
                settings.JsonSerializer = new WeiboJsonSerializer(jsonSettings);
                //settings.UrlEncodedSerializer = new WeiboUrlEncodedSerializer();
                settings.BeforeCall = call =>
                {
                    call.Request.Headers.Add("Cookie",
                        string.Join(";",
                            Singleton<Api>.Instance.GetCookies().Select(it => $"{it.Key}={it.Value}")));
                };
                settings.OnErrorAsync = async call =>
                {
                    if (call.HttpStatus == HttpStatusCode.Forbidden)
                    {
                        //maybe errno:100005
                        var json = await call.Response.Content.ReadAsStringAsync();
                        try
                        {
                            call.FlurlRequest.Client.Settings.JsonSerializer.Deserialize<JObject>(json);
                        }
                        catch (FlurlParsingException e) when (e.InnerException is WeiboException)
                        {
                            //TODO: show notification
                        }
                    }
                };
            });
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ImageCache.Instance.InitializeAsync(httpMessageHandler: new WeiboHttpClientHandler());
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args is ProtocolActivatedEventArgs protocolActivatedEventArgs)
            {
                EnsureWindow();
                Singleton<BroadcastCenter>.Instance.SendWithPendingMessage(this, "share_target_receive", protocolActivatedEventArgs);
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            EnsureWindow(e.PrelaunchActivated);
        }

        private void EnsureWindow(bool preLaunch = false)
        {
            if (!(Window.Current.Content is RootView))
            {
                Window.Current.Content = new RootView();
            }

            if (preLaunch == false)
            {
                Window.Current.Activate();
            }
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (!(Window.Current.Content is ShareTargetView))
            {
                Window.Current.Content = new ShareTargetView(args);
            }
            Window.Current.Activate();
        }
    }
}