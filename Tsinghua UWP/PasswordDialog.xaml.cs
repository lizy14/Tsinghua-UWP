﻿using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TsinghuaUWP {
    public sealed partial class PasswordDialog : ContentDialog {

        public async Task<Password> getCredentialAsyc(
            bool validate = true        /* true: perform validation, only return validated values */) {
            while (true) {
                var userResponse = await this.ShowAsync();

                if (userResponse != ContentDialogResult.Primary) {

                    var userRasponseAgain = await (new ContentDialog {
                        Title = "是否继续",
                        Content = @"
如果不提供学号和密码，课程表、
作业 ddl 提醒功能将不可用。

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
                if (!isNumeric) {
                    await (new ContentDialog {
                        Title = "学号应为纯数字",
                        PrimaryButtonText = "返回"
                    }).ShowAsync();
                    continue;
                }
                if (username.Length == 0 || password.Length == 0) {
                    await (new ContentDialog {
                        Title = "学号、密码不能为空",
                        PrimaryButtonText = "返回"
                    }).ShowAsync();
                    continue;
                }

                try {
                    if (validate) {
                        await Remote.validateCredential(username, password);
                    }
                    return new Password {
                        username = username,
                        password = password
                    };
                } catch (LoginException e) {
                    await (new ContentDialog {
                        Title = "登录失败",
                        Content = $@"
服务器返回信息：
{e.Message}",
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                } catch (Exception e) {
                    await (new ContentDialog {
                        Title = "登录出错",
                        Content = Exceptions.getFriendlyMessage(e),
                        PrimaryButtonText = "重试"
                    }).ShowAsync();
                }

            }


        }

        public PasswordDialog() {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

    }
}
