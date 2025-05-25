using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Enums.Metadata.Announcement;
using ClassIsland.Core.Models.Metadata.Announcement;
using ClassIsland.Helpers;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Metadata;

public class AnnouncementService : ObservableRecipient, IAnnouncementService
{
    public ILogger<AnnouncementService> Logger { get; }


    public AnnouncementService(ILogger<AnnouncementService> logger)
    {
        Logger = logger;

        var keyStream = Application
            .GetResourceStream(new Uri("/Assets/TrustedPublicKeys/ClassIsland.MetadataPublisher.asc", UriKind.RelativeOrAbsolute))!.Stream;
        MetadataPublisherPublicKey = new StreamReader(keyStream).ReadToEnd();

        UpdateReadAnnouncements();
        AnnouncementsInternal =
            ConfigureFileHelper.LoadConfig<ObservableCollection<Announcement>>(Path.Combine(App.AppCacheFolderPath,
                "Announcements.json"));
        _ = RefreshAnnouncements();
    }

    private ObservableCollection<Announcement> _announcementsInternal = [];
    private ObservableCollection<Guid> _readAnnouncementsMachine = [];
    private ObservableCollection<Guid> _readAnnouncementsLocal = [];
    public IReadOnlyList<Announcement> Announcements => AnnouncementsInternal.Where(x => x.EndTime >= DateTime.Now &&
        x.StartTime <= DateTime.Now &&
        (!ReadAnnouncementsMachine.Contains(x.Guid) || x.ReadStateStorageScope is ReadStateStorageScope.Local) &&
        (!ReadAnnouncementsLocal.Contains(x.Guid) || x.ReadStateStorageScope is ReadStateStorageScope.Machine)).ToList();

    private string MetadataPublisherPublicKey { get; set; }

    private ObservableCollection<Announcement> AnnouncementsInternal
    {
        get => _announcementsInternal;
        set
        {
            if (Equals(value, _announcementsInternal)) return;
            _announcementsInternal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Announcements));
        }
    }

    public async Task RefreshAnnouncements()
    {
        try
        {
            var time = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            AnnouncementsInternal =
                await WebRequestHelper.SaveJson<ObservableCollection<Announcement>>(new Uri($"https://get.classisland.tech/d/ClassIsland-Ningbo-S3/classisland/announcements.json?time={time}"), Path.Combine(App.AppCacheFolderPath,
                    "Announcements.json"), verifySign: true, publicKey: MetadataPublisherPublicKey);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "刷新公告失败");
        }
        
    }

    public ObservableCollection<Guid> ReadAnnouncementsLocal
    {
        get => _readAnnouncementsLocal;
        private set
        {
            if (Equals(value, _readAnnouncementsLocal)) return;
            _readAnnouncementsLocal = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Guid> ReadAnnouncementsMachine
    {
        get => _readAnnouncementsMachine;
        private set
        {
            if (Equals(value, _readAnnouncementsMachine)) return;
            _readAnnouncementsMachine = value;
            OnPropertyChanged();
        }
    }

    private void UpdateReadAnnouncements()
    {
        ReadAnnouncementsLocal.CollectionChanged -= ReadAnnouncementsOnCollectionChanged;
        ReadAnnouncementsMachine.CollectionChanged -= ReadAnnouncementsOnCollectionChanged;

        ReadAnnouncementsLocal = ConfigureFileHelper.LoadConfig<ObservableCollection<Guid>>(Path.Combine(
            App.AppConfigPath,
            "ReadAnnouncements.json"));
        ReadAnnouncementsMachine = ConfigureFileHelper.LoadConfig<ObservableCollection<Guid>>(Path.Combine(
            App.AppDataFolderPath,
            "ReadAnnouncements.json"));

        ReadAnnouncementsLocal.CollectionChanged += ReadAnnouncementsOnCollectionChanged;
        ReadAnnouncementsMachine.CollectionChanged += ReadAnnouncementsOnCollectionChanged;
    }

    private void SaveReadAnnouncements()
    {
        ConfigureFileHelper.SaveConfig(Path.Combine(
            App.AppConfigPath,
            "ReadAnnouncements.json"), ReadAnnouncementsLocal);
        ConfigureFileHelper.SaveConfig(Path.Combine(
            App.AppDataFolderPath,
            "ReadAnnouncements.json"), ReadAnnouncementsMachine);
    }

    private void ReadAnnouncementsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SaveReadAnnouncements();
        OnPropertyChanged(nameof(Announcements));
    }
}