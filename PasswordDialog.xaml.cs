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
            bool validate = true        /* true: perform validation, only return validated values */)
        {
            while (true)
            {
                var userResponse = await this.ShowAsync();
                
                if (userResponse != ContentDialogResult.Primary) {

                    var userRasponseAgain = await (new ContentDialog
                    {
                        Title = "是否继续",
                        Content = @"
如果不提供学号和密码，将无法从学校服
务器获取课表、课程公告、作业等信息。

您可以在任何时候选择重新登录。
",
                        PrimaryButtonText = "继续",
                        SecondaryButtonText = "现在登录"
                    }).ShowAsync();
                    if (userRasponseAgain == ContentDialogResult.Primary)
                        throw new UserCancelException();
                    else
                        continue;
                }
                var username = this.username.Text;
                var password = this.password.Password;

                int n;
                bool isNumeric = int.TryParse(username, out n);
                if (!isNumeric)
                {
                    await (new ContentDialog
                    {
                        Title = "学号应为纯数字",
                        PrimaryButtonText = "返回"
                    }).ShowAsync();
                    continue;
                }
                if (username.Length == 0 || password.Length == 0)
                {
                    await (new ContentDialog
                    {
                        Title = "学号、密码不能为空",
                        PrimaryButtonText = "返回"
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
