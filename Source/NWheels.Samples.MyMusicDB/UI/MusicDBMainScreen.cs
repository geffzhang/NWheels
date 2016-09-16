﻿using NWheels.Domains.Security;
using NWheels.Domains.Security.UI;
using NWheels.Extensions;
using NWheels.Samples.MyMusicDB.Authorization;
using NWheels.Samples.MyMusicDB.Domain;
using NWheels.UI;
using NWheels.UI.Toolbox;
using NWheels.UI.Uidl;

namespace NWheels.Samples.MyMusicDB.UI
{
    public class MusicDBMainScreen : ScreenBase<MusicDBMainScreen, Empty.Input, Empty.Data, Empty.State>
    {
        public MusicDBMainScreen(string idName, UidlApplication parent)
            : base(idName, parent)
        {
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void DescribePresenter(PresenterBuilder<MusicDBMainScreen, Empty.Data, Empty.State> presenter)
        {
            ContentRoot = Console;
            Console
                .Dashboard(this.Dashboard)
                .StatusBarWidgets(CurrentUser)
                .Navigation(new {
                    Dashboard = Goto(this.Dashboard).Appearance(icon: "th-large"),
                    MusicDB = new { 
                        @this = new {
                            Icon = "database"
                        },
                        Genres = Goto(this.Genres).Appearance(icon: "sitemap"),
                        Artists = Goto(this.Artists).Appearance(icon: "user"),
                        Albums = Goto(this.Albums).Appearance(icon: "headphones"),
                        Tracks = Goto(this.Tracks).Appearance(icon: "play-circle")
                    },
                    System = new {
                        @this = new {
                            Icon = "gear",
                            Authorization = new UidlAuthorization(MusicDBClaims.UserRoleAdministrator)
                        },
                        UserAccounts = Goto(this.Users).Appearance(icon: "lock"),
                        SystemLog = Popup(App.SystemLog).Appearance(icon: "list"),
                    },
                    Logout = Menu.Action.InvokeCommand(CurrentUser.LogOut).Appearance(icon: "sign-out")
                });

            Console.ProfilePhoto.SetEntity<IProfilePhotoEntity>(x => x.ImageType, x => x.ImageContents);
            Console.ProfilePhoto.Bind(vm => vm.State.ProfilePhotoId).ToGlobalAppState<MusicDBApp.IState>(x => x.LoggedInUser.ProfilePhotoId);
            Console.ProfilePhoto.Bind(vm => vm.State.PersonFullName).ToGlobalAppState<MusicDBApp.IState>(x => x.LoggedInUser.FullName);
            Console.ProfilePhoto.Bind(vm => vm.State.UserType).ToGlobalAppState<MusicDBApp.IState>(x => x.LoggedInUser.UserType);
            Console.ProfilePhoto.Bind(vm => vm.State.UserId).ToGlobalAppState<MusicDBApp.IState>(x => x.LoggedInUser.UserId);

            presenter.On(Console.ProfilePhoto.LogOut).InvokeCommand(CurrentUser.LogOut);
            presenter.On(Console.ProfilePhoto.ChangePassword).Navigate().FromContainer(Console.MainContent).ToScreenPartNonTyped(ChangePassword);

            Genres.Crud.Grid
                .Column(x => x.Id, size: FieldSize.Small)
                .Column(x => x.Name);

            Artists.Crud.Grid
                .Column(x => x.Id, size: FieldSize.Small)
                .Column(x => x.Name)
                .Column(x => x.Description, size: FieldSize.ExtraLarge);
            Artists.Crud.Form
                .Field(x => x.Description, type: FormFieldType.Edit, modifiers: FormFieldModifiers.Memo)
                .Field(x => x.Genres, setup: f => {
                    var genreCrud = (Crud<IGenreRelation>)f.NestedWidget;
                    genreCrud.Grid
                        .Column(x => x.GenreName, title: "Genre", size: FieldSize.Large)
                        .Column(x => x.IsPrimary, title: "Primary", size: FieldSize.Small)
                        .Column(x => x.IsRecommended, title: "Recommended", size: FieldSize.Small);
                    genreCrud.Form
                        .ShowFields(x => x.Genre, x => x.IsPrimary, x => x.IsRecommended);
                })
                .ShowFields(x => x.Id, x => x.Name, x => x.Description, x => x.Genres);

            Albums.Crud.Grid
                .Column(x => x.Id, size: FieldSize.Small)
                .Column(x => x.Name)
                .Column(x => x.Artist.Name, title: "Artist")
                .Column(x => x.ReleaseYear, size: FieldSize.Small)
                .Column(x => x.Description, size: FieldSize.ExtraLarge);
            Albums.Crud.Form
                .Field(x => x.Tracks, setup: f => {
                    var trackCrud = (Crud<ITrackEntity>)f.NestedWidget;
                    trackCrud.Grid
                        .Column(x => x.TrackNumber, size: FieldSize.Small)
                        .Column(x => x.Name)
                        .Column(x => x.Length, size: FieldSize.Small)
                        .Column(x => x.Description, size: FieldSize.ExtraLarge);
                    trackCrud.Form
                        .Field(x => x.AlbumText, label: "Album")
                        .ShowFields(x => x.Id, x => x.AlbumText, x => x.Name, x => x.TrackNumber, x => x.Length, x => x.Description);
                })
                .Field(x => x.CoverImageUrl, label: "CoverImageURL")
                .ShowFields(x => x.Id, x => x.Name, x => x.Description, x => x.Artist, x => x.CoverImageUrl, x => x.ReleaseYear, x => x.Description);

            Tracks.Crud.Grid
                .Column(x => x.Id, size: FieldSize.Small)
                .Column(x => x.ArtistName, title: "Artist")
                .Column(x => x.AlbumName, title: "Album")
                .Column(x => x.TrackNumber, title: "Track", size: FieldSize.Small)
                .Column(x => x.Name, size: FieldSize.Large)
                .Column(x => x.Length, size: FieldSize.Small)
                .Column(x => x.Description, size: FieldSize.ExtraLarge);
            Tracks.Crud.Form
                .Field(x => x.AlbumText, label: "Album")
                .ShowFields(x => x.Id, x => x.AlbumText, x => x.Name, x => x.TrackNumber, x => x.Length, x => x.Description);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------
        
        public MusicDBApp App { get; set; }
        public ManagementConsole Console { get; set; }
        public LoggedInUserWidget CurrentUser { get; set; }
        public MusicDBDashboardScreenPart Dashboard { get; set; }
        public CrudScreenPart<IGenreEntity> Genres { get; set; }
        public CrudScreenPart<IArtistEntity> Artists { get; set; }
        public CrudScreenPart<IAlbumEntity> Albums { get; set; }
        public CrudScreenPart<ITrackEntity> Tracks { get; set; }
        public UserAccountCrudScreenPart<IMusicDBUserAccountEntity> Users { get; set; }
        public ChangePasswordScreenPart ChangePassword { get; set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private Menu.ItemAction Goto(IScreenPartWithInput<object> screenPart, params string[] userRoles)
        {
            return Menu.Action.Goto(screenPart, this.Console.MainContent).UserRoles(userRoles);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private Menu.ItemAction Goto(IScreenPartWithInput<Empty.Input> screenPart, params string[] userRoles)
        {
            return Menu.Action.Goto(screenPart, this.Console.MainContent).UserRoles(userRoles);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------

        private Menu.ItemAction Popup<TInput>(IScreenWithInput<TInput> screen, params string[] userRoles)
        {
            return Menu.Action.PopupScreen(screen).UserRoles(userRoles);
        }
    }
}
