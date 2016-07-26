using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TsinghuaUWP
{
    public sealed partial class PasswordDialog : ContentDialog
    {

        public async Task<Password> getCredentialAsyc(
            bool noCancel = false    /* true: ask again and again; false: throws UserCancelException */,
            bool validate = true        /* true: perform validation, only return validated values */)
        {
            while (true)
            {
                var userResponse = await this.ShowAsync();
                
                if (userResponse != ContentDialogResult.Primary) {
                    if (noCancel)
                    {
                        await (new ContentDialog {
                            Title = "无法继续",
                            Content = @"
需要您的用户名和密码，以从网络学堂获取课程公告、作业、校历等信息。
",
                            PrimaryButtonText = "确定"
                        }).ShowAsync();
                        continue;
                    }
                    else
                        throw new UserCancelException();
                }
                var username = this.username.Text;
                var password = this.password.Password;
                if(username.Length == 0 || password.Length == 0)
                {
                    await (new ContentDialog
                    {
                        Title = "用户名、密码不能为空",
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                    continue;
                }

                try
                {
                    if (validate)
                    {
                        await Remote.login(false, username, password);
                    }
                    return new Password
                    {
                        username = username,
                        password = password
                    };
                }
                catch(LoginException e) {
                    await (new ContentDialog
                    {
                        Title = "登录失败",
                        Content = $@"
服务器返回信息：
{e.Message}",
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                }
                catch(ParsePageException e)
                {
                    await (new ContentDialog
                    {
                        Title = "登录失败",
                        Content = e.verbose(),
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                }
                catch (Exception e)
                {
                    string msg = Exceptions.getFriendlyMessage(e);
                    if (!NetworkInterface.GetIsNetworkAvailable())
                        msg = $"网络不可用 ({msg})";

                    await (new ContentDialog
                    {
                        Title = "登录出错",
                        Content = $@"
{msg}",
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                }


            }


        }

        public PasswordDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        
    }
}
