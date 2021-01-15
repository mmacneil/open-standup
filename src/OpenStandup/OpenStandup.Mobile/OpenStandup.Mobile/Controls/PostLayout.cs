﻿using System;
using System.Threading.Tasks;
using Autofac;
using OpenStandup.Common.Dto;
using OpenStandup.Core.Interfaces;
using OpenStandup.Core.Interfaces.Apis;
using OpenStandup.Mobile.Converters;
using OpenStandup.Mobile.Factories;
using OpenStandup.Mobile.Helpers;
using OpenStandup.Mobile.Interfaces;
using OpenStandup.Mobile.ViewModels;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Xamarin.CommunityToolkit.Effects;
using Xamarin.Forms;

namespace OpenStandup.Mobile.Controls
{
    public class PostLayout : Grid
    {
        private readonly IAppContext _appContext = App.Container.Resolve<IAppContext>();
        private readonly IAppSettings _appSettings = App.Container.Resolve<IAppSettings>();
        private readonly IDialogProvider _dialogProvider = App.Container.Resolve<IDialogProvider>();
        private readonly IOpenStandupApi _openStandupApi = App.Container.Resolve<IOpenStandupApi>();
        private readonly IPageFactory _pageFactory = App.Container.Resolve<IPageFactory>();
        private readonly IPopupNavigation _popupNavigation = App.Container.Resolve<IPopupNavigation>();
        private readonly IToastService _toastService = App.Container.Resolve<IToastService>();

        public PostLayout(PostViewMode viewMode, Func<Task> deleteHandler, bool hasImage = false)
        {
            Padding = new Thickness(0, 10);
            RowSpacing = 0;

            this.SetBinding(AutomationIdProperty, nameof(PostSummaryDto.Id));

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var avatar = new Image { Aspect = Aspect.AspectFill };
            avatar.SetBinding(Image.SourceProperty, nameof(PostSummaryDto.AvatarUrl));

            var loginLabel = new Label { Style = ResourceDictionaryHelper.GetStyle("LinkLabel"), VerticalOptions = LayoutOptions.Center };
            loginLabel.SetBinding(Label.TextProperty, nameof(PostSummaryDto.Login));
            loginLabel.SetBinding(AutomationIdProperty, nameof(PostSummaryDto.GitHubId));

            var loginTapGestureRecognizer = new TapGestureRecognizer();
            loginTapGestureRecognizer.Tapped += async (sender, args) =>
            {
                await _popupNavigation.PushAsync(_pageFactory.Resolve<ProfileViewModel>(vm =>
                {
                    vm.SelectedGitHubId = loginLabel.AutomationId;
                    vm.SelectedLogin = loginLabel.Text;
                }) as PopupPage);
            };

            loginLabel.GestureRecognizers.Add(loginTapGestureRecognizer);

            var modifiedLabel = new Label { Style = ResourceDictionaryHelper.GetStyle("MetaLabel"), VerticalOptions = LayoutOptions.Center };
            modifiedLabel.SetBinding(Label.TextProperty, nameof(PostSummaryDto.Modified));

            Children.Add(new StackLayout { Padding = new Thickness(13, 0), Children = { new RoundImage(avatar, 35, 35, 20), loginLabel, modifiedLabel }, Orientation = StackOrientation.Horizontal, HorizontalOptions = LayoutOptions.Start });

            var contentLayout = new StackLayout();

            // Alert: Padding borks the TapGestureRecognizer on hyper-linked spans but Margin works
            var textLabel = new Label { LineHeight = 1.1, Margin = new Thickness(15, 15, 15, 8) };
            textLabel.SetBinding(Label.FormattedTextProperty, nameof(PostSummaryDto.HtmlText), BindingMode.Default, new HtmlLabelConverter());

            contentLayout.Children.Add(textLabel);

            if (hasImage)
            {
                var image = new Image
                {
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 200,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                image.SetBinding(IsVisibleProperty, nameof(PostSummaryDto.ImageName), BindingMode.Default, new StringToBoolConverter());

                image.SetBinding(Image.SourceProperty, new Binding(nameof(PostSummaryDto.ImageName), BindingMode.Default,
                    new StringToUriImageSourceConverter(),
                    $"{_appSettings.Host}/images/posts/"));

                contentLayout.Children.Add(image);
            }

            Children.Add(contentLayout, 0, 1);

            var commentLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    new Label { Style = ResourceDictionaryHelper.GetStyle("MetaIcon"), Text = IconFont.CommentMultiple },
                    new Label { Text = "0",  Style = ResourceDictionaryHelper.GetStyle("MetaText") }
                }
            };

            if (viewMode == PostViewMode.Summary)
            {
                TouchEffect.SetNativeAnimation(commentLayout, true);
                TouchEffect.SetCommand(commentLayout, new Command(async () =>
                {
                    await _popupNavigation.PushAsync(_pageFactory.Resolve<PostDetailViewModel>(vm => vm.Id = Convert.ToInt32(AutomationId)) as PopupPage);
                }));
            }

            var deleteLayout = new StackLayout
            {
                Children =
                {
                    new Label { Style = ResourceDictionaryHelper.GetStyle("MetaIcon"), Text = IconFont.Delete },
                    new Label { Text = "delete",  Style = ResourceDictionaryHelper.GetStyle("MetaCommandText") }
                },
                Style = ResourceDictionaryHelper.GetStyle("MetaCommandLayout")
            };

            deleteLayout.SetBinding(IsVisibleProperty, new Binding(nameof(PostSummaryDto.GitHubId), BindingMode.Default, new UserIdIsMeBoolConverter(), _appContext.User.Id));

            TouchEffect.SetNativeAnimation(deleteLayout, true);
            TouchEffect.SetCommand(deleteLayout, new Command(async () =>
            {
                if (!await _dialogProvider.DisplayAlert("Delete", "Delete this post?", "Yes", "No"))
                {
                    return;
                }

                await _openStandupApi.DeletePost(Convert.ToInt32(AutomationId));
                await deleteHandler();
                _toastService.Show("Post deleted");
            }));

            Children.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 15,
                Padding = new Thickness(15, 0, 0, 0),
                Children =
                {
                    commentLayout,
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Children =
                        {
                            new Label { Style = ResourceDictionaryHelper.GetStyle("MetaIcon"), Text = IconFont.ThumbUp },
                            new Label { Text = "0", Style = ResourceDictionaryHelper.GetStyle("MetaText") }
                        }
                    },
                    deleteLayout
                },
            }, 0, 2);

            Children.Add(new BoxView
            {
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 1,
                Color = Color.FromHex("#d9dadc")
            }, 0, 3);
        }
    }
}


