using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;

namespace LumioApp;

public partial class App : Application
{
    public App()
    {
        MainPage = new NavigationPage(new LumioMainPage())
        {
            BarBackgroundColor = Color.FromArgb("#06080d"),
            BarTextColor = Colors.White
        };
    }
}

public class LumioMainPage : ContentPage
{
    private const string WebsiteUrl = "https://github.io";
    private const string ProfileDownloadUrl = "https://github.iolumio-lab.mobileconfig";
    private const string RemoteGameApiUrl = "https://github.ioapi/play.html";

    private readonly VerticalStackLayout _contentArea;
    private readonly Label _statusBadge;
    private bool _profileInstalled = false;

    public LumioMainPage()
    {
        Title = "LUMIO";
        BackgroundColor = Color.FromArgb("#06080d");

        _statusBadge = new Label
        {
            Text = "● SYSTEM CHECKING...",
            TextColor = Color.FromArgb("#22d3ee"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        _contentArea = new VerticalStackLayout
        {
            Spacing = 16,
            Padding = new Thickness(20)
        };

        var footerLink = new Label
        {
            Text = "Visit Official Website (Nexora Interactive Technologies)",
            TextColor = Color.FromArgb("#818cf8"),
            FontSize = 12,
            TextDecorations = TextDecorations.Underline,
            HorizontalOptions = LayoutOptions.Center
        };
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) => await Launcher.Default.OpenAsync(new Uri(WebsiteUrl));
        footerLink.GestureRecognizers.Add(tapGesture);

        var copyrightLabel = new Label
        {
            Text = "Copyright © 2026 Nexora Interactive Technologies, Lumio.\nLumio is owned by Nexora Interactive Technologies. All rights reserved, no part of this website, app and/or other stuff inside Nexora Interactive Technologies can be copied by any way.",
            TextColor = Color.FromArgb("#64748b"),
            FontSize = 10,
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Padding = new Thickness(0, 15, 0, 30),
                Children =
                {
                    new Label
                    {
                        Text = "LUMIO",
                        FontSize = 34,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    _statusBadge,
                    _contentArea,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#1e293b"), Margin = new Thickness(20, 10) },
                    footerLink,
                    copyrightLabel
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RunStartupVerificationAsync();
    }
    private async Task RunStartupVerificationAsync()
    {
        _contentArea.Children.Clear();
        _statusBadge.Text = "● RUNNING DIAGNOSTICS...";
        _statusBadge.TextColor = Color.FromArgb("#22d3ee");

        // 1. CHECK WI-FI CONNECTION
        var access = Connectivity.Current.NetworkAccess;
        var profiles = Connectivity.Current.ConnectionProfiles;
        bool isWifi = profiles.Contains(ConnectionProfile.WiFi);

        if (access != NetworkAccess.Internet || !isWifi)
        {
            ShowWifiErrorScreen();
            return;
        }
        // 2. REQUEST & VERIFY ALWAYS + PRECISE LOCATION
        var whenInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (whenInUseStatus != PermissionStatus.Granted)
            whenInUseStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        var alwaysLocationStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (alwaysLocationStatus != PermissionStatus.Granted)
            alwaysLocationStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();

        bool isPrecise = true;
#if IOS
        var manager = new CoreLocation.CLLocationManager();
        isPrecise = manager.AccuracyAuthorization == CoreLocation.CLAccuracyAuthorization.FullAccuracy;
#endif

        if (alwaysLocationStatus != PermissionStatus.Granted || !isPrecise)
        {
            ShowMissingErrorScreen(
                errorCode: "ERR-101",
                missingName: "Always & Precise Location",
                tutorial: "1. Open your iPhone Settings app.\n2. Scroll down and tap Lumio.\n3. Tap Location and select 'Always'.\n4. Make sure the 'Precise Location' switch is toggled ON.\n5. Return to Lumio and tap Re-Verify."
            );
            return;
        }


        var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        if (bluetoothStatus != PermissionStatus.Granted)
            bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();

        if (bluetoothStatus != PermissionStatus.Granted)
        {
            ShowMissingErrorScreen(
                errorCode: "ERR-102",
                missingName: "Bluetooth Permission",
                tutorial: "1. Open your iPhone Settings app.\n2. Scroll down and tap Lumio.\n3. Toggle the Bluetooth switch ON.\n4. Return to Lumio and tap Retry."
            );
            return;
        }

        var sensorsStatus = await Permissions.CheckStatusAsync<Permissions.Sensors>();
        if (sensorsStatus != PermissionStatus.Granted)
            sensorsStatus = await Permissions.RequestAsync<Permissions.Sensors>();

        if (sensorsStatus != PermissionStatus.Granted)
        {
            ShowMissingErrorScreen(
                errorCode: "ERR-103",
                missingName: "Motion & Fitness Sensors",
                tutorial: "1. Open Settings > Privacy & Security > Motion & Fitness.\n2. Make sure Fitness Tracking and Lumio are turned ON.\n3. Return to Lumio and tap Retry."
            );
            return;
        }

        // 3. DOWNLOAD & VERIFY CONFIGURATION PROFILE
        if (!_profileInstalled)
        {
            ShowProfileInstallScreen();
            return;
        }

        // 4. CONNECT TO REMOTE API FOR GAMES
        await ConnectToApiGamesAsync();
    }

    private void ShowWifiErrorScreen()
    {
        _statusBadge.Text = "✕ NETWORK DISCONNECTED";
        _statusBadge.TextColor = Color.FromArgb("#ef4444");

        _contentArea.Children.Add(CreateCard(
            title: "404 Not connected to the internet",
            body: "You are not connected to any wifi. Please make sure that you are connected to continue.",
            borderColor: "#ef4444",
            buttonText: "Retry Wi-Fi Check",
            onClick: async () => await RunStartupVerificationAsync()
        ));
    }

    private void ShowMissingErrorScreen(string errorCode, string missingName, string tutorial)
    {
        _statusBadge.Text = $"✕ {errorCode} VERIFICATION FAILED";
        _statusBadge.TextColor = Color.FromArgb("#ef4444");

        string fullMessage = $"[{errorCode}] Files or permission missing!\n\n{missingName} is missing!\n\nHow to fix it:\n{tutorial}";

        _contentArea.Children.Add(CreateCard(
            title: $"{errorCode} — Missing Requirement",
            body: fullMessage,
            borderColor: "#ef4444",
            buttonText: "I Fixed It — Re-Verify",
            onClick: async () => await RunStartupVerificationAsync()
        ));
    }

    private void ShowProfileInstallScreen()
    {
        _statusBadge.Text = "⚠ CONFIGURATION PROFILE REQUIRED";
        _statusBadge.TextColor = Color.FromArgb("#f59e0b");

        var card = new Frame
        {
            BackgroundColor = Color.FromArgb("#0d111a"),
            BorderColor = Color.FromArgb("#6366f1"),
            CornerRadius = 14,
            Padding = 20,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Step 1: Download & Install Lumio Profile", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label
                    {
                        Text = "Tap 'Download Profile' below. When Safari asks, tap Allow, then open Settings > General > VPN & Device Management to install the Lumio Profile.",
                        TextColor = Color.FromArgb("#94a3b8"),
                        FontSize = 14
                    },
                    new Button
                    {
                        Text = "1. Download Configuration Profile",
                        BackgroundColor = Color.FromArgb("#6366f1"),
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        Command = new Command(async () => await Launcher.Default.OpenAsync(new Uri(ProfileDownloadUrl)))
                    },
                    new Button
                    {
                        Text = "2. Verify Profile & Continue",
                        BackgroundColor = Color.FromArgb("#10b981"),
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        Command = new Command(async () =>
                        {
                            _profileInstalled = true;
                            await RunStartupVerificationAsync();
                        })
                    }
                }
            }
        };

        _contentArea.Children.Add(card);
    }

    private async Task ConnectToApiGamesAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(RemoteGameApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("API not ready");
            }

            _statusBadge.Text = "● CONNECTED TO LUMIO API";
            _statusBadge.TextColor = Color.FromArgb("#10b981");

            _contentArea.Children.Add(new WebView
            {
                Source = RemoteGameApiUrl,
                HeightRequest = 540,
                BackgroundColor = Color.FromArgb("#06080d")
            });
        }
        catch
        {
            ShowMissingErrorScreen(
                errorCode: "ERR-404-API",
                missingName: "Lumio Game API",
                tutorial: "The remote API endpoint is not online yet. Once you add your API instructions, tap Re-Verify below."
            );
        }
    }

    private Frame CreateCard(string title, string body, string borderColor, string buttonText, Func<Task> onClick)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb("#0d111a"),
            BorderColor = Color.FromArgb(borderColor),
            CornerRadius = 14,
            Padding = 20,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = body, FontSize = 14, TextColor = Color.FromArgb("#cbd5e1") },
                    new Button
                    {
                        Text = buttonText,
                        BackgroundColor = Color.FromArgb("#6366f1"),
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        Command = new Command(async () => await onClick())
                    }
                }
            }
        };
    }
}
