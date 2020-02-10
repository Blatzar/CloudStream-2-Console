﻿
using Jint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
//using Android.Util;
//using Android.Content;
using System.Threading.Tasks;
using static CloudStream2Console.CloudStreamCore;


namespace CloudStream2Console
{
    class Program
    {
        const string SEARCH_FOR_PREFIX = "Search: ";
        const bool LIVE_SEARCH = false;

        /// <summary>
        /// 0 = Search, 1 = EPView, 2 = Links
        /// </summary>
        static int currentView = 0;
        static string currentSearch = "";
        static List<Poster> searchPosters = new List<Poster>();
        static List<Episode> currentEpisodes = new List<Episode>();
        static int currentSeason = 1;
        static int epSelect = -1;
        static bool isDub = true;
        static bool dubExists = false;
        static bool subExists = false;
        static int maxEpisodes = -1;
        static int loadLinkEpisodeSelected = -1;
        static int currentMaxEpisodes { get { if (currentMovie.title.movieType == MovieType.Anime) { return Math.Min(maxEpisodes, currentEpisodes.Count); } else { return currentEpisodes.Count; } } }

        static Movie currentMovie = new Movie();
        static int selected = -1;

        static void PrintSearch()
        {
            Console.Clear();

            string s(int sel)
            {
                return (selected == sel ? "> " : "");
            }

            Console.WriteLine(s(-1) + SEARCH_FOR_PREFIX + currentSearch);
            for (int i = 0; i < searchPosters.Count; i++) {
                Console.WriteLine(s(i) + searchPosters[i].name + " (" + (searchPosters[i].year) + ")");
            }
        }


        static bool IsEnglishLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
        static bool IsEnglishNumber(char c)
        {
            return (c >= '0' && c <= '9');
        }

        static List<Link> currentLinks = new List<Link>();

        static void Main(string[] args)
        {
            CloudStreamCore.searchLoaded += (o, e) => {
                if (e != searchPosters) {
                    if (currentView == 0) {
                        searchPosters = e;
                        selected = -1;
                        PrintSearch();
                    }
                }
            };

            void PrintCurrentTitle()
            {
                Console.Clear();
                Console.WriteLine(currentMovie.title.name + " IMDb:" + currentMovie.title.rating + " (" + currentMovie.title.year + ")");
            }

            void RenderEpisodes()
            {
                if (currentMovie.title.movieType == MovieType.Anime) {
                    Console.WriteLine((epSelect == -2 ? "< " : "") + (isDub ? "Dub" : "Sub") + (epSelect == -2 ? " >" : ""));
                }
                if (!currentMovie.title.IsMovie) {
                    Console.WriteLine((epSelect == -1 ? "< " : "") + "Season " + currentSeason + (epSelect == -1 ? " >" : ""));
                }

                for (int i = 0; i < currentMaxEpisodes; i++) {
                    var ep = currentEpisodes[i];
                    Console.WriteLine((epSelect == i ? "> " : "") + (currentMovie.title.IsMovie ? "" : (i + 1) + ". ") + ep.name + (currentMovie.title.IsMovie ? "" : " (" + ep.rating + ")"));
                };
            }

            void RenderEveryTitle()
            {
                if (currentMovie.title.id == null) {
                    Console.Clear();
                    Console.WriteLine("Loading ");
                }
                else {
                    PrintCurrentTitle();
                    RenderEpisodes();
                }
            }

            CloudStreamCore.titleLoaded += (o, e) => {
                currentEpisodes = new List<Episode>();
                currentMovie = e;
                if (currentView == 1) {
                    PrintCurrentTitle();
                    currentSeason = 1;
                    CloudStreamCore.GetImdbEpisodes();
                }
            };

            CloudStreamCore.linkAdded += (o, e) => {
    
                
                if (currentView == 2) {
                    currentLinks.Add(e);
                   // var ep = curret//e[loadLinkEpisodeSelected];
                    Console.Clear();
                    //var links = ep.links.OrderBy(t => -t.priority).ToList();
                    for (int i = 0; i < currentLinks.Count; i++) {
                        Console.WriteLine(currentLinks[i].url + "\n");
                    }
                }
            };

            static void SetDubSub()
            {
                maxEpisodes = GetMaxEpisodesInAnimeSeason(currentMovie, currentSeason, isDub);
            }

            CloudStreamCore.episodeLoaded += (o, e) => {
                currentMovie = activeMovie;
                //  currentMovie.episodes = e;
                currentEpisodes = e;

                // DUB SUB LOGIC
                if (currentMovie.title.movieType == MovieType.Anime) {
                    dubExists = false;
                    subExists = false;
                    try {
                        for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                            MALSeason ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q];
                            try {
                                if (ms.dubbedAnimeData.dubExists) {
                                    dubExists = true;
                                }
                            }
                            catch (Exception) {
                            }
                            try {
                                if (ms.gogoData.dubExists) {
                                    dubExists = true;
                                }
                                if (ms.gogoData.subExists) {
                                    subExists = true;
                                }
                            }
                            catch (Exception) {
                            }
                        }
                    }
                    catch (Exception) {
                    }

                    isDub = dubExists;
                    SetDubSub();
                    print("MAXEPISDES:" + maxEpisodes + "| DUBEX:" + dubExists + "| SUBEX" + subExists);
                }
                RenderEveryTitle();

            };

            void Search()
            {
                QuickSearch(currentSearch);
                selected = -1;
                searchPosters = new List<Poster>();
            }
            void SwitchDubState()
            {
                if (isDub && subExists) {
                    isDub = false;
                }
                else if (!isDub && dubExists) {
                    isDub = true;
                }
            }

            while (true) {
                var input = Console.ReadKey();
                char f = input.KeyChar;

                int epSelectfloor = -2;
                if (currentView == 1) {

                    if (currentMovie.title.IsMovie) {
                        epSelectfloor = 0;
                    }
                    else if (currentMovie.title.movieType != MovieType.Anime) {
                        epSelectfloor = -1;
                    }
                }



                switch (input.Key) {
                    case ConsoleKey.Escape:
                        currentView--;
                        if (currentView < 0) currentView = 0;
                        if(currentView == 0) { selected = -1; }
                      //  if(currentView == 1) { epSelect = -1; }
                        break;
                    case ConsoleKey.DownArrow:
                        if (currentView == 0) {
                            selected++;
                            if (selected >= searchPosters.Count) {
                                selected = -1;
                            }
                        }
                        else if (currentView == 1) {
                            epSelect++;
                            if (epSelect >= currentMaxEpisodes) {
                                epSelect = epSelectfloor;
                            }
                        }
                        // handle left arrow
                        break;
                    case ConsoleKey.UpArrow:
                        if (currentView == 0) {
                            selected--;
                            if (selected < -1) {
                                selected = searchPosters.Count - 1;
                            }
                        }
                        else if (currentView == 1) {
                            epSelect--;
                            if (epSelect < epSelectfloor) {
                                epSelect = currentMaxEpisodes - 1;
                            }
                        }
                        // handle right arrow
                        break;
                    case ConsoleKey.Enter:
                        if (currentView == 0) {
                            if (selected != -1) {
                                currentView = 1;
                                epSelect = -1;
                                currentMovie = new Movie();
                                CloudStreamCore.GetImdbTitle(searchPosters[selected], autoSearchTrailer: false);
                                Console.Clear();
                                Console.WriteLine("Loading");
                            }
                            else {
                                Search();
                            }
                        }
                        else if (currentView == 1) {
                            if (epSelect == -1) {
                                currentEpisodes = new List<Episode>();
                                CloudStreamCore.GetImdbEpisodes(currentSeason);
                            }
                            else if (epSelect == -2) {
                                SetDubSub();
                            }
                            else {
                                currentView = 2;
                                loadLinkEpisodeSelected = epSelect;
                                currentLinks = new List<Link>();
                                GetEpisodeLink(currentMovie.title.IsMovie ? -1 : epSelect + 1, currentSeason, isDub: isDub);
                            }
                        }

                        // handle right arrow
                        break;
                    case ConsoleKey.RightArrow:
                        if (currentView == 1) {
                            if (epSelect == -1) {
                                currentSeason++;
                                if (currentSeason > currentMovie.title.seasons) {
                                    currentSeason = 1;
                                }
                            }
                            else if (epSelect == -2) {
                                SwitchDubState();
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (currentView == 1) {
                            if (epSelect == -1) {
                                currentSeason--;
                                if (currentSeason <= 0) {
                                    currentSeason = currentMovie.title.seasons;
                                }
                            }
                            else if (epSelect == -2) {
                                SwitchDubState();
                            }
                        }
                        break;
                }
                if (currentView == 0) {
                    bool search = false;
                    if (f == '') { // DELETE
                        search = true;
                        if (currentSearch.Length >= 1) {
                            currentSearch = currentSearch.Substring(0, currentSearch.Length - 1);
                        }
                    }
                    else if (IsEnglishLetter(f) || f == ' ' || IsEnglishNumber(f)) {
                        currentSearch += f;
                        search = true;
                    }
                    if (search) {
                        if (LIVE_SEARCH) {
                            Search();
                        }

                    }
                    PrintSearch();
                }
                else if (currentView == 1) {
                    RenderEveryTitle();
                    print("sel:" + epSelect);
                }
            }
        }
    }



    public static class App
    {
        public static int ConvertDPtoPx(int dp)
        {
            return dp * 4;
        }

        public static void ShowToast(string toast)
        {

        }

        public static int GetSizeOfJumpOnSystem()
        {
            return 1024;
        }

        static string GetKeyPath(string folder, string name = "")
        {
            string _s = ":" + folder + "-";
            if (name != "") {
                _s += name + ":";
            }
            return _s;
        }
        static Dictionary<string, object> Properties = new Dictionary<string, object>();
        public static void SetKey(string folder, string name, object value)
        {
            string path = GetKeyPath(folder, name);
            if (Properties.ContainsKey(path)) {
                Properties[path] = value;
            }
            else {
                Properties.Add(path, value);
            }
        }

        public static T GetKey<T>(string folder, string name, T defVal)
        {
            string path = GetKeyPath(folder, name);
            return GetKey<T>(path, defVal);
        }

        public static void RemoveFolder(string folder)
        {
            List<string> keys = App.GetKeysPath(folder);
            for (int i = 0; i < keys.Count; i++) {
                RemoveKey(keys[i]);
            }
        }

        public static T GetKey<T>(string path, T defVal)
        {
            if (Properties.ContainsKey(path)) {
                return (T)Properties[path];
            }
            else {
                return defVal;
            }
        }

        public static List<T> GetKeys<T>(string folder)
        {
            List<string> keyNames = GetKeysPath(folder);

            List<T> allKeys = new List<T>();
            foreach (var key in keyNames) {
                allKeys.Add((T)Properties[key]);
            }

            return allKeys;
        }

        public static int GetKeyCount(string folder)
        {
            return GetKeysPath(folder).Count;
        }
        public static List<string> GetKeysPath(string folder)
        {
            List<string> keyNames = Properties.Keys.Where(t => t.StartsWith(GetKeyPath(folder))).ToList();
            return keyNames;
        }

        public static bool KeyExists(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            return KeyExists(path);
        }
        public static bool KeyExists(string path)
        {
            return (Properties.ContainsKey(path));
        }
        public static void RemoveKey(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            RemoveKey(path);
        }
        public static void RemoveKey(string path)
        {
            if (Properties.ContainsKey(path)) {
                Properties.Remove(path);
            }
        }
    }

    public static class Settings
    {
        public static bool SubtitlesEnabled = false;
        public static bool DefaultDub = true;
        public static bool CacheImdb = true;
        public static bool CacheMAL = true;
    }

    [Serializable]
    public static class CloudStreamCore
    {

        // ========================================================= CONSTS =========================================================
        #region CONSTS
        public const bool MOVIES_ENABLED = true;
        public const bool TVSERIES_ENABLED = true;
        public const bool ANIME_ENABLED = true;

        public const bool CHROMECAST_ENABLED = true;
        public const bool DOWNLOAD_ENABLED = true;
        public const bool SEARCH_FOR_UPDATES_ENABLED = true;

        public const bool INLINK_SUBTITLES_ENABLED = false;
        public static bool globalSubtitlesEnabled { get { return Settings.SubtitlesEnabled; } }
        public const bool GOMOSTEAM_ENABLED = true;
        public const bool SUBHDMIRROS_ENABLED = true;
        public const bool FMOVIES_ENABLED = false;
        public const bool BAN_SUBTITLE_ADS = true;

        public const bool PLAY_SELECT_ENABLED = false;

        public const bool DEBUG_WRITELINE = true;
        public const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
        public const int MIRROR_COUNT = 10; // SUB HD MIRRORS


        public const string loadingImage = "https://i.giphy.com/media/u2Prjtt7QYD0A/200.webp"; // from https://media0.giphy.com/media/u2Prjtt7QYD0A/200.webp?cid=790b7611ff76f40aaeea5e73fddeb8408c4b018b6307d9e3&rid=200.webp

        public const bool REPLACE_IMDBNAME_WITH_POSTERNAME = true;
        public static double posterRezMulti = 1.0;
        public const string GOMOURL = "gomo.to";
        #endregion

        // ========================================================= THREDS =========================================================

        #region Threads
        public static List<int> activeThredIds = new List<int>();
        public static List<TempThred> tempThreds = new List<TempThred>();
        public static int thredNumber = 0; // UNIQUE THRED NUMBER FOR EATCH THREAD CREATED WHEN TEMPTHREDS THRED IS SET

        /// <summary>
        /// Same as thred.Join(); but removes refrense in tempThreds 
        /// </summary>
        /// <param name="tempThred"></param>
        public static void JoinThred(TempThred tempThred)
        {
            try {
                activeThredIds.Remove(tempThred.ThredId);

            }
            catch (Exception) {
            }
            GetThredActive(tempThred);
        }

        public static bool GetThredActive(TempThred tempThred, bool autoPurge = true)
        {
            bool active = activeThredIds.Contains(tempThred.ThredId);
            if (!active && autoPurge) { PurgeThred(tempThred); }
            return active;
        }

        public static void PurgeThred(TempThred tempThred)
        {
            try {
                // print("Purged: " + tempThred.ThredId);
                activeThredIds.Remove(tempThred.ThredId);
                tempThreds.Remove(tempThred);
                tempThred.Thread.Abort();

                // print(tempThred.Thread.Name);
                /*
                try {

                    if (DeviceInfo.Platform == DevicePlatform.UWP) {
                        tempThred.Thread.Join();
                        tempThred.Thread.Abort();

                    }
                    else {
                        //tempThred.Thread.Join();

                    }

                }
                catch (Exception) {
                }*/


            }
            catch (Exception) {

            }

        }

        /// <summary>
        ///  THRED ID IS THE THREDS POURPOSE
        ///  0=Normal, 1=SEARCHTHRED, 2=GETTITLETHREAD, 3=LINKTHRED, 4=DOWNLOADTHRED, 5=TRAILERTHREAD, 6=EPISODETHREAD
        /// </summary>
        public static void PurgeThreds(int typeId)
        {
            print("PURGING ALL THREADS TYPE OF: " + typeId);
            if (typeId == -1) {
                activeThredIds = new List<int>();
                tempThreds = new List<TempThred>();
            }
            else {
                //  List<int> _activeThredIds = new List<int>();
                List<TempThred> _tempThreds = new List<TempThred>();
                foreach (TempThred t in tempThreds) {
                    if (t.typeId == typeId) {
                        //  PurgeThred(t);
                        _tempThreds.Add(t);
                    }
                    /*
                    if (t.typeId != typeId) {
                        _tempThreds.Add(t);
                        _activeThredIds.Add(t.ThredId);
                    }
                    else {
                        try {

                        }
                        catch (Exception) {

                            throw;
                        }
                        t.Thread.Abort();
                    }
                    */
                }
                for (int i = 0; i < _tempThreds.Count; i++) {
                    PurgeThred(_tempThreds[i]);
                }
                //activeThredIds = _activeThredIds;
                //  tempThreds = _tempThreds;
            }
        }
        #endregion

        // ========================================================= DATA =========================================================

        #region Data
        [Serializable]

        public enum MovieType { Movie, TVSeries, Anime, AnimeMovie }
        [Serializable]
        public enum PosterType { Imdb, Raw }

        [Serializable]
        public struct FMoviesData
        {
            public string url;
            public int season;
        }

        [Serializable]
        public struct TempThred
        {
            /// <summary>
            /// THRED ID IS THE THREDS POURPOSE
            /// 0=Normal, 1=SEARCHTHRED, 2=GETTITLETHREAD, 3=LINKTHRED, 4=DOWNLOADTHRED, 5=TRAILERTHREAD, 6=EPISODETHREAD
            /// </summary>
            public int typeId;

            private int _thredId;
            /// <summary>
            /// THR ID IF THE THRED (UNIQUE)
            /// </summary>
            public int ThredId { private set { _thredId = value; } get { return _thredId; } }

            public System.Threading.Thread _thread;
            public System.Threading.Thread Thread
            {
                set {
                    if (_thread == null) {
                        thredNumber++; _thredId = thredNumber; activeThredIds.Add(_thredId); tempThreds.Add(this);
                    }
                    _thread = value;
                }
                get { return _thread; }
            }
        }

        [Serializable]
        public struct IMDbTopList
        {
            public string name;
            public string id;
            public string img;
            public string runtime;
            public string rating;
            public string genres;
            public string descript;
            public int place;
            public List<int> contansGenres;
        }

        [Serializable]
        public struct Trailer
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string PosterUrl { get; set; }
        }

        [Serializable]
        public struct FishWatch
        {
            public string imdbScore;
            public string title;
            public string removedTitle;
            public int season;
            public string released;
            public string href;
        }

        [Serializable]
        public struct Movies123
        {
            public string year;
            public string imdbRating;
            public string genre;
            public string plot;
            public string runtime;
            public string posterUrl;
            public string name;
            public MovieType type;
        }

        [Serializable]
        public struct MALSeason
        {
            public string name;
            public string japName;
            public string engName;
            public List<string> synonyms;

            public GogoAnimeData gogoData;
            public DubbedAnimeData dubbedAnimeData;
        }
        [Serializable]
        public struct GogoAnimeData
        {
            public bool dubExists;
            public bool subExists;
            public string subUrl;
            public string dubUrl;

        }
        [Serializable]

        public struct DubbedAnimeData
        {
            public bool dubExists;
            public string slug;
        }

        [Serializable]
        public struct MALSeasonData
        {
            public List<MALSeason> seasons;
            public string malUrl;
        }

        [Serializable]
        public struct MALData
        {
            public string japName;
            public string engName;
            public List<MALSeasonData> seasonData;
            public bool done;
            public bool loadSeasonEpCountDone;
            public List<int> currentActiveGoGoMaxEpsPerSeason;
            public List<int> currentActiveDubbedMaxEpsPerSeason;
            public string currentSelectedYear;
        }

        [System.Serializable]
        public struct DubbedAnimeEpisode
        {
            public int rowid;
            public string title;
            public string desc;
            public string status;
            public int totalEp;
            public int ep;
            public string slug;
            public int year;
            public int showid;
            public int Epviews;
            public int TotalViews;
            public string serversHTML;
        }

        [Serializable]
        public struct Title
        {
            public string name;
            public string ogName;
            //public string altName;
            public string id;
            public string year;
            public string ogYear => year.Substring(0, 4);
            public string rating;
            public string runtime;
            public string posterUrl;
            public string description;
            public int seasons;
            public string hdPosterUrl;

            public MALData MALData;

            public MovieType movieType;
            public List<string> genres;
            public List<Trailer> trailers;
            public List<Poster> recomended;

            public Movies123MetaData movies123MetaData;
            public List<YesmoviessSeasonData> yesmoviessSeasonDatas; // NOT SORTED; MAKE SURE TO SEARCH ALL

            public List<WatchSeriesHdMetaData> watchSeriesHdMetaData;// NOT SORTED; MAKE SURE TO SEARCH ALL
            public List<FMoviesData> fmoviesMetaData;// NOT SORTED; MAKE SURE TO SEARCH ALL

            public string shortEpView;

            public bool IsMovie { get { return (movieType == MovieType.AnimeMovie || movieType == MovieType.Movie); } }
        }

        [Serializable]
        public struct YesmoviessSeasonData
        {
            public string url;
            public int id;
        }

        [Serializable]
        public struct Movies123MetaData
        {
            public string movieLink;
            public List<Movies123SeasonData> seasonData;
        }

        [Serializable]
        public struct WatchSeriesHdMetaData
        {
            public string url;
            public int season;
        }

        [Serializable]
        public struct Movies123SeasonData
        {
            public string seasonUrl;
            public List<string> episodeUrls;
        }

        [Serializable]
        public struct Poster
        {
            public string name;
            public string extra; // (Tv-Series) for exampe
            public string posterUrl;
            public string year;
            public string rank;
            //public string id; // IMDB ID

            public string url;
            public PosterType posterType; // HOW DID YOU GET THE POSTER, IMDB SEARCH OR SOMETHING ELSE
        }

        [Serializable]
        public struct Link
        {
            public string name;
            public string url;
            public int priority;
        }

        [Serializable]
        public struct Episode
        {
            public List<Link> links;
            public string name;
            public string rating;
            public string description;
            public string date;
            public string posterUrl;
            public string id;

            //private int _progress;
            // public int Progress { set { _progress = value; linkAdded?.Invoke(null, value); } get { return _progress; } }
        }

        [Serializable]
        public struct Subtitle
        {
            public string name;
            //public string url;
            public string data;
        }

        [Serializable]
        public struct Movie
        {
            public Title title;
            public List<Subtitle> subtitles;
            public List<Episode> episodes;
        }
        #endregion

        // ========================================================= EVENTS =========================================================

        #region Events
        public static List<Poster> activeSearchResults = new List<Poster>();
        public static Movie activeMovie = new Movie();
        public static string activeTrailer = "";

        public static event EventHandler<Poster> addedSeachResult;
        public static event EventHandler<Movie> titleLoaded;
        public static event EventHandler<List<Poster>> searchLoaded;
        public static event EventHandler<List<Trailer>> trailerLoaded;
        public static event EventHandler<List<Episode>> episodeLoaded;
        public static event EventHandler<Link> linkAdded;
        public static event EventHandler<MALData> malDataLoaded;
        public static event EventHandler<Episode> linksProbablyDone;

        public static event EventHandler<Movie> movie123FishingDone;
        public static event EventHandler<Movie> yesmovieFishingDone;
        public static event EventHandler<Movie> watchSeriesFishingDone;
        public static event EventHandler<Movie> fmoviesFishingDone;
        public static event EventHandler<Movie> fishingDone;
        //public static event EventHandler<Movie> yesmovieFishingDone;

        private static Random rng = new Random();
        #endregion

        // ========================================================= ALL METHODS =========================================================

        /// <summary>
        /// Get a shareble url of the current movie
        /// </summary>
        /// <param name="extra"></param>
        /// <param name="redirectingName"></param>
        /// <returns></returns>
        public static string ShareMovieCode(string extra, string redirectingName = "Redirecting to CloudStream 2")
        {
            const string baseUrl = "CloudStreamForms";
            //Because I don't want to host my own servers I "Save" a js code on a free js hosting site. This code will automaticly give a responseurl that will redirect to the CloudStream app.
            string code = ("var x = document.createElement('body');\n var s = document.createElement(\"script\");\n s.innerHTML = \"window.location.href = '" + baseUrl + ":" + extra + "';\";\n var h = document.createElement(\"H1\");\n var div = document.createElement(\"div\");\n div.style.width = \"100%\";\n div.style.height = \"100%\";\n div.align = \"center\";\n div.style.padding = \"130px 0\";\n div.style.margin = \"auto\";\n div.innerHTML = \"" + redirectingName + "\";\n h.append(div);\n x.append(h);\n x.append(s);\n parent.document.body = x;").Replace("%", "%25");
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create("https://js.do/mod_perl/js.pl");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            string postData = "action=save_code&js_code=" + code + "&js_title=&js_permalink=&js_id=&is_update=false";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            // Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            reader.Close();
            dataStream.Close();
            response.Close();
            string rLink = "https://js.do/code/" + FindHTML(responseFromServer, "js_permalink\":\"", "\"");
            return rLink;
            // Clean up the streams.
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<IMDbTopList> FetchRecomended(List<string> inp, bool shuffle = true, int max = 10)
        {
            List<IMDbTopList> topLists = new List<IMDbTopList>();

            Shuffle(inp);
            if (inp.Count > max) {
                inp.RemoveRange(max, inp.Count - max);
            }

            for (int q = 0; q < inp.Count; q++) {
                string url = "https://www.imdb.com/title/" + inp[q];

                //string d =;
                string _d = GetHTML(url);
                string lookFor = "<div class=\"rec_item\"";
                while (_d.Contains(lookFor)) {
                    _d = RemoveOne(_d, lookFor);
                    string tt = FindHTML(_d, " data-tconst=\"", "\"");
                    string name = FindHTML(_d, "alt=\"", "\"", decodeToNonHtml: true);
                    string img = FindHTML(_d, "loadlate=\"", "\"");
                    string d = RemoveOne(_d, "<a href=\"/title/" + tt + "/vote?v=X;k", -200);
                    string __d = FindHTML(_d, "<div class=\"rec-title\">\n       <a href=\"/title/" + tt, "<div class=\"rec-rating\">");
                    List<string> genresNames = new List<string>() { "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };
                    List<int> contansGenres = new List<int>();
                    for (int i = 0; i < genresNames.Count; i++) {
                        if (__d.Contains(genresNames[i])) {
                            contansGenres.Add(i);
                        }
                    }
                    string value = FindHTML(d, "<span class=\"value\">", "<");
                    string descript = FindHTML(d, "<div class=\"rec-outline\">\n    <p>\n    ", "<");
                    if (!value.Contains(".")) {
                        value += ".0";
                    }

                    bool add = true;
                    for (int z = 0; z < topLists.Count; z++) {
                        if (topLists[z].id == tt) {

                            add = false;
                        };
                    }

                    if (add) {
                        topLists.Add(new IMDbTopList() { name = name, descript = descript, contansGenres = contansGenres, id = tt, img = img, place = -1, rating = value, runtime = "", genres = "" });
                    }
                    else {
                    }
                }
            }

            if (shuffle) {
                Shuffle<IMDbTopList>(topLists);
            }

            return topLists;
        }

        public static List<IMDbTopList> FetchTop100(List<string> order, int start = 1, int count = 250)
        {
            IMDbTopList[] topLists = new IMDbTopList[count];
            //List<string> genres = new List<string>() { "action", "adventure", "animation", "biography", "comedy", "crime", "drama", "family", "fantasy", "film-noir", "history", "horror", "music", "musical", "mystery", "romance", "sci-fi", "sport", "thriller", "war", "western" };
            //List<string> genresNames = new List<string>() { "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };
            string orders = "";
            for (int i = 0; i < order.Count; i++) {
                if (i != 0) {
                    orders += ",";
                }
                orders += order[i];
            }
            //https://www.imdb.com/search/title/?genres=adventure&sort=user_rating,desc&title_type=feature&num_votes=25000,&pf_rd_m=A2FGELUUNOQJNL&pf_rd_p=5aab685f-35eb-40f3-95f7-c53f09d542c3&pf_rd_r=VV0XPKMS8FXZ6D8MM0VP&pf_rd_s=right-6&pf_rd_t=15506&pf_rd_i=top&ref_=chttp_gnr_2
            //https://www.imdb.com/search/title/?title_type=feature&num_votes=25000,&genres=action&sort=user_rating,desc&start=51&ref_=adv_nxt
            string trueUrl = "https://www.imdb.com/search/title/?title_type=feature&num_votes=25000,&genres=" + orders + "&sort=user_rating,desc&start=" + start + "&ref_=adv_nxt&count=" + count;
            print("TRUEURL:" + trueUrl);
            string d = GetHTML(trueUrl, true);
            print("FALSEURL:" + trueUrl);

            string lookFor = "class=\"loadlate\"";
            int place = start - 1;
            int counter = 0;
            while (d.Contains(lookFor)) {
                place++;
                d = RemoveOne(d, lookFor);

                string img = FindHTML(d, "loadlate=\"", "\"");
                string id = FindHTML(d, "data-tconst=\"", "\"");
                string runtime = FindHTML(d, "<span class=\"runtime\">", "<");
                string name = FindHTML(d, "ref_=adv_li_tt\"\n>", "<");
                string rating = FindHTML(d, "</span>\n        <strong>", "<");
                string _genres = FindHTML(d, "<span class=\"genre\">\n", "<").Replace("  ", "");
                string descript = FindHTML(d, "<p class=\"text-muted\">\n    ", "<").Replace("  ", "");
                topLists[counter] = (new IMDbTopList() { descript = descript, genres = _genres, id = id, img = img, name = name, place = place, rating = rating, runtime = runtime });
                counter++;
            }
            print("------------------------------------ DONE! ------------------------------------");
            return topLists.ToList();
        }

        public static void QuickSearch(string text, bool purgeCurrentSearchThread = true, bool onlySearch = true)
        {

            if (purgeCurrentSearchThread) {
                PurgeThreds(1);
            }

            TempThred tempThred = new TempThred();

            tempThred.typeId = 1;

            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                    text = rgx.Replace(text, "").ToLower();
                    if (text == "") {
                        return;
                    }
                    string qSearchLink = "https://v2.sg.media-imdb.com/suggestion/" + text.Substring(0, 1) + "/" + text.Replace(" ", "_") + ".json";
                    string result = DownloadString(qSearchLink, tempThred);
                    //print(qSearchLink+ "|" +result);
                    string lookFor = "{\"i\":{\"";

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    activeSearchResults = new List<Poster>();

                    int counter = 0;
                    while (result.Contains(lookFor) && counter < 100) {
                        counter++;
                        string name = ReadJson(result, "\"l");
                        name = RemoveHtmlChars(name);
                        string posterUrl = ReadJson(result, "imageUrl");
                        string extra = ReadJson(result, "\"q");
                        string year = FindHTML(result, "\"y\":", "}"); string oyear = year;
                        string years = FindHTML(year, "yr\":\"", "\""); if (years.Length > 4) { year = years; }
                        string id = ReadJson(result, "\"id");
                        string rank = FindHTML(result, "rank\":", ",");
                        if (extra == "feature") { extra = ""; }

                        if (year != "" && id.StartsWith("tt") && !extra.Contains("video game")) {
                            AddToActiveSearchResults(new Poster() { name = name, posterUrl = posterUrl, extra = extra, year = year, rank = rank, url = id, posterType = PosterType.Imdb });
                        }
                        result = RemoveOne(result, "y\":" + oyear);
                    }

                    if (onlySearch) {
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        searchLoaded?.Invoke(null, activeSearchResults);
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "QuickSearch";
            tempThred.Thread.Start();
        }

        public static string RemoveHtmlChars(string inp)
        {
            return System.Net.WebUtility.HtmlDecode(inp);
        }

        public static void GetWatchTV(int season, int episode, int normalEpisode)
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 1; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string url = "https://www.tvseries.video/series/" + ToDown(activeMovie.title.name, replaceSpace: "-") + "/" + "season-" + season + "-episode-" + episode;

                    string d = DownloadString(url);
                    string vidId = FindHTML(d, " data-vid=\"", "\"");
                    if (vidId != "") {
                        d = DownloadString("https://www.tvseries.video" + vidId);
                        AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "FishWatch";
            tempThred.Thread.Start();
        }

        public static void GetMALData(bool cacheData = true)
        {
            bool fetchData = true;
            if (Settings.CacheMAL) {
                if (App.KeyExists("CacheMAL", activeMovie.title.id)) {
                    fetchData = false;
                    activeMovie.title.MALData = App.GetKey<MALData>("CacheMAL", activeMovie.title.id, new MALData());
                    if (activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason == null) {
                        fetchData = true;
                    }
                }
            }
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string currentSelectedYear = "";

                    if (fetchData) {
                        string year = activeMovie.title.year.Substring(0, 4); // this will not work in 8000 years time :)
                        string _d = DownloadString("https://myanimelist.net/search/prefix.json?type=anime&keyword=" + activeMovie.title.name, tempThred);
                        string url = "";
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        string lookFor = "\"name\":\"";
                        bool done = false;
                        while (_d.Contains(lookFor) && !done) { // TO FIX MY HERO ACADIMEA CHOOSING THE SECOND SEASON BECAUSE IT WAS FIRST SEARCHRESULT
                            string name = FindHTML(_d, lookFor, "\"");
                            print("NAME FOUND: " + name);
                            string _url = FindHTML(_d, "url\":\"", "\"").Replace("\\/", "/");
                            string startYear = FindHTML(_d, "start_year\":", ",");
                            string aired = FindHTML(_d, "aired\":\"", "\"");
                            string _aired = FindHTML(aired, ", ", " ", readToEndOfFile: true);
                            string score = FindHTML(_d, "score\":\"", "\"");
                            print("SCORE:" + score);
                            if (!name.Contains(" Season") && year == _aired && score != "N\\/A") {
                                print("URL FOUND: " + _url);
                                print(_d);
                                url = _url;
                                done = true;
                                currentSelectedYear = _aired;
                            }
                            _d = RemoveOne(_d, lookFor);
                            _d = RemoveOne(_d, "\"id\":");
                        }

                        /*

                        string d = DownloadString("https://myanimelist.net/search/all?q=" + activeMovie.title.name);

                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        d = RemoveOne(d, " <div class=\"picSurround di-tc thumb\">"); // DONT DO THIS USE https://myanimelist.net/search/prefix.json?type=anime&keyword=my%20hero%20acadimea
                        string url = "";//"https://myanimelist.net/anime/" + FindHTML(d, "<a href=\"https://myanimelist.net/anime/", "\"");
                        */

                        if (url == "") return;

                        WebClient webClient = new WebClient();
                        webClient.Encoding = Encoding.UTF8;

                        string d = webClient.DownloadString(url);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        string jap = FindHTML(d, "Japanese:</span> ", "<").Replace("  ", "").Replace("\n", ""); // JAP NAME IS FOR SEARCHING, BECAUSE ALL SEASONS USE THE SAME NAME
                        string eng = FindHTML(d, "English:</span> ", "<").Replace("  ", "").Replace("\n", "");

                        string currentName = FindHTML(d, "<span itemprop=\"name\">", "<");
                        List<MALSeasonData> data = new List<MALSeasonData>() { new MALSeasonData() { malUrl = url, seasons = new List<MALSeason>() } };

                        string sqlLink = "-1";

                        // ----- GETS ALL THE SEASONS OF A SHOW WITH MY ANIME LIST AND ORDERS THEM IN THE CORRECT SEASON (BOTH Shingeki no Kyojin Season 3 Part 2 and Shingeki no Kyojin Season 3 is season 3) -----
                        while (sqlLink != "") {
                            string _malLink = (sqlLink == "-1" ? url.Replace("https://myanimelist.net", "") : sqlLink);
                            currentName = FindHTML(d, "<span itemprop=\"name\">", "<", decodeToNonHtml: true);
                            string sequel = FindHTML(d, "Sequel:", "</a></td>") + "<";
                            sqlLink = FindHTML(sequel, "<a href=\"", "\"");
                            string _jap = FindHTML(d, "Japanese:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "");
                            string _eng = FindHTML(d, "English:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "");
                            string _syno = FindHTML(d, "Synonyms:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "") + ",";
                            List<string> _synos = new List<string>();
                            while (_syno.Contains(",")) {
                                string _current = _syno.Substring(0, _syno.IndexOf(",")).Replace("  ", "");
                                if (_current.StartsWith(" ")) {
                                    _current = _current.Substring(1, _current.Length - 1);
                                }
                                _synos.Add(_current);
                                _syno = RemoveOne(_syno, ",");
                            }
                            print("CURRENTNAME: " + currentName + "|" + _eng + "|" + _jap);

                            if (currentName.Contains("Part ") && !currentName.Contains("Part 1")) // WILL ONLY WORK UNTIL PART 10, BUT JUST HOPE THAT THAT DOSENT HAPPEND :)
                            {
                                data[data.Count - 1].seasons.Add(new MALSeason() { name = currentName, engName = _eng, japName = _jap, synonyms = _synos });
                            }
                            else {
                                data.Add(new MALSeasonData() {
                                    seasons = new List<MALSeason>() { new MALSeason() { name = currentName, engName = _eng, japName = _jap, synonyms = _synos } },
                                    malUrl = "https://myanimelist.net" + _malLink
                                });
                            }
                            if (sqlLink != "") {
                                try {
                                    d = webClient.DownloadString("https://myanimelist.net" + sqlLink);
                                }
                                catch (Exception) {
                                    d = "";
                                }
                            }
                        }
                        for (int i = 0; i < data.Count; i++) {
                            for (int q = 0; q < data[i].seasons.Count; q++) {
                                var e = data[i].seasons[q];
                                string _s = "";
                                for (int z = 0; z < e.synonyms.Count; z++) {
                                    _s += e.synonyms[z] + "|";
                                }
                                print("SEASON: " + (i + 1) + "  -  " + e.name + "|" + e.engName + "|" + e.japName + "| SYNO: " + _s);
                            }
                        }
                        activeMovie.title.MALData = new MALData() {
                            seasonData = data,
                            japName = jap,
                            engName = eng,
                            done = false,
                            currentSelectedYear = currentSelectedYear,
                        };
                        if (fetchData && cacheData && Settings.CacheMAL) {
                            App.SetKey("CacheMAL", activeMovie.title.id, activeMovie.title.MALData);
                        }
                    }
                    else {
                        currentSelectedYear = activeMovie.title.MALData.currentSelectedYear;
                    }
                    print("FISHING DATA");
                    FishGogoAnime(currentSelectedYear, tempThred);
                    FishDubbedAnime();
                    print("DONE FISHING DATA");
                    MALData md = activeMovie.title.MALData;

                    activeMovie.title.MALData = new MALData() {
                        seasonData = md.seasonData,
                        japName = md.japName,
                        done = true,
                    };

                    //print(sequel + "|" + realSquel + "|" + sqlLink);

                }
                catch {
                    activeMovie.title.MALData.japName = "error";
                    // throw;
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "MAL Search";
            tempThred.Thread.Start();
        }

        static void FishDubbedAnime()
        {
            string _imdb = activeMovie.title.name; //"Attack On Titan";
            string imdb = _imdb.Replace(".", "").Replace("/", "");
            string d = DownloadString("https://bestdubbedanime.com/search/" + imdb);
            string lookFor = "class=\"resulta\" href=\"";
            string nameLookFor = "<div class=\"titleresults\">";
            print(d);

            List<int> alreadyAdded = new List<int>();
            while (d.Contains(nameLookFor)) {
                string name = FindHTML(d, nameLookFor, "<", decodeToNonHtml: true);
                if (name.ToLower().Contains(_imdb.ToLower())) {

                    string url = FindHTML(d, lookFor, "\"").Replace("\\/", "/");
                    string slug = url.Replace("//bestdubbedanime.com/", "");

                    int season = 0;
                    if (name.ToLower().Contains("2nd season")) {
                        season = 2;
                    }
                    else if (name.ToLower().Contains("3rd season")) {
                        season = 3;
                    }
                    if (season == 0) {
                        for (int i = 1; i < 7; i++) {
                            if (name.EndsWith(" " + i)) {
                                season = i;
                            }
                        }
                    }
                    if (season == 0) {
                        season = 1;
                    }
                    int part = 1;
                    for (int i = 2; i < 5; i++) {
                        if (name.ToLower().Contains("part " + i)) {
                            part = i;
                        }
                    }


                    int id = season + part * 1000;
                    if (!alreadyAdded.Contains(id)) {
                        alreadyAdded.Add(id);
                        try {
                            print("SEASON::" + season + "PART" + part);
                            var ms = activeMovie.title.MALData.seasonData[season].seasons[part - 1];
                            ms.dubbedAnimeData.dubExists = true;
                            ms.dubbedAnimeData.slug = slug;
                            activeMovie.title.MALData.seasonData[season].seasons[part - 1] = ms;
                            print("ÖÖ>>");
                            print(activeMovie.title.MALData.seasonData[season].seasons[part - 1].dubbedAnimeData.dubExists);
                        }
                        catch (Exception) {
                            print("ERROR IN " + "SEASON::" + season + "PART" + part);
                            //throw;
                            // ERROR
                        }
                    }

                    print("-->" + name + "|" + url + "| Season " + season + "|" + slug + "|Park" + part);

                    //print("Season " + season + "||" + slug);
                }
                d = RemoveOne(d, nameLookFor);
            }
        }

        static void FishGogoAnime(string currentSelectedYear, TempThred tempThred)
        {
            print("start");
            if (activeMovie.title.MALData.japName != "error") {
                print("DOWNLOADING");
                string d = DownloadString("https://www9.gogoanime.io/search.html?keyword=" + activeMovie.title.MALData.japName.Substring(0, Math.Min(5, activeMovie.title.MALData.japName.Length)), tempThred);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                string look = "<p class=\"name\"><a href=\"/category/";

                while (d.Contains(look)) {
                    string ur = FindHTML(d, look, "\"").Replace("-dub", "");
                    print("S" + ur);
                    string adv = FindHTML(d, look, "</a");
                    string title = FindHTML(adv, "title=\"", "\"").Replace(" (TV)", ""); // TO FIX BLACK CLOVER
                    string animeTitle = title.Replace(" (Dub)", "");
                    string __d = RemoveOne(d, look);
                    string __year = FindHTML(__d, "Released: ", " ");
                    int ___year = int.Parse(__year);
                    int ___year2 = int.Parse(currentSelectedYear);

                    if (___year >= ___year2) {

                        // CHECKS SYNONYMES
                        /*
                        for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                            for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                                MALSeason ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                            }
                        }*/

                        // LOADS TITLES
                        for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                            for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                                MALSeason ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                                bool containsSyno = false;
                                for (int s = 0; s < ms.synonyms.Count; s++) {
                                    if (ToLowerAndReplace(ms.synonyms[s]) == ToLowerAndReplace(animeTitle)) {
                                        containsSyno = true;
                                    }
                                    //  print("SYNO: " + ms.synonyms[s]);
                                }

                                //  print(animeTitle.ToLower() + "|" + ms.name.ToLower() + "|" + ms.engName.ToLower() + "|" + ___year + "___" + ___year2 + "|" + containsSyno);

                                if (ToLowerAndReplace(ms.name) == ToLowerAndReplace(animeTitle) || ToLowerAndReplace(ms.engName) == ToLowerAndReplace(animeTitle) || containsSyno) {
                                    print(ur);
                                    var baseData = activeMovie.title.MALData.seasonData[i].seasons[q];
                                    if (animeTitle == title) {
                                        baseData.gogoData.subExists = true;
                                        baseData.gogoData.subUrl = ur;

                                    }
                                    else {
                                        baseData.gogoData.dubExists = true;
                                        baseData.gogoData.dubUrl = ur.Replace("-dub", "") + "-dub";
                                    }

                                    /*
                                    if (animeTitle == title) {
                                         //= new MALSeason() { name = ms.name, subUrl = ur, dubUrl = ms.dubUrl, subExists = true, dubExists = ms.dubExists, japName = ms.japName, engName = ms.engName, synonyms = ms.synonyms };
                                    }
                                    else {
                                        activeMovie.title.MALData.seasonData[i].seasons[q] //= new MALSeason() { name = ms.name, dubUrl = ur.Replace("-dub", "") + "-dub", subUrl = ms.subUrl, dubExists = true, subExists = ms.subExists, japName = ms.japName, engName = ms.engName, synonyms = ms.synonyms };
                                    }*/
                                    activeMovie.title.MALData.seasonData[i].seasons[q] = baseData;

                                }
                            }
                        }
                    }
                    d = d.Substring(d.IndexOf(look) + 1, d.Length - d.IndexOf(look) - 1);
                }
                for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                    for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                        var ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                        if (ms.gogoData.dubExists) {
                            print(i + ". " + ms.name + " | Dub E" + ms.gogoData.dubUrl);
                        }
                        if (ms.gogoData.subExists) {
                            print(i + ". " + ms.name + " | Sub E" + ms.gogoData.subUrl);
                        }
                    }
                }
            }
        }

        public static string ToLowerAndReplace(string inp)
        {
            return inp.ToLower().Replace("-", " ").Replace("`", "\'");
        }

        public static void GetImdbTitle(Poster imdb, bool purgeCurrentTitleThread = true, bool autoSearchTrailer = true, bool cacheData = true)
        {
            string __id = imdb.url.Replace("https://imdb.com/title/", "");
            bool fetchData = true;
            if (Settings.CacheImdb) {
                print("START CACHE");
                if (App.KeyExists("CacheImdb", __id)) {
                    print("KEY EXIST CACHE");

                    fetchData = false;
                    activeMovie = App.GetKey<Movie>("CacheImdb", __id, new Movie());
                    print("KEY EXIST MMM");
                    print(":::::::::" + activeMovie.title.name);

                    if (activeMovie.title.name == null || activeMovie.title.id == null) {
                        fetchData = true;
                    }
                }
            }

            if (purgeCurrentTitleThread) {
                PurgeThreds(2);
            }
            if (fetchData) {
                activeMovie = new Movie();
                activeMovie.title.id = __id;
            }
            // TurnNullMovieToActive(movie);
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string d = "";
                    List<string> keyWords = new List<string>();

                    if (fetchData) {
                        string url = "https://imdb.com/title/" + imdb.url.Replace("https://imdb.com/title/", "") + "/";
                        d = GetHTML(url); // DOWNLOADSTRING WILL GET THE LOCAL LAUNGEGE, AND NOT EN, THAT WILL MESS WITH RECOMENDATIONDS, GetHTML FIXES THAT
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        string _d = DownloadString(url + "keywords", tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        string _lookFor = "data-item-keyword=\"";
                        while (_d.Contains(_lookFor)) {
                            keyWords.Add(FindHTML(_d, _lookFor, "\""));
                            _d = RemoveOne(_d, _lookFor);
                        }
                        for (int i = 0; i < keyWords.Count; i++) {
                            // print("Keyword: " + keyWords[i]);
                        }
                    }
                    if (d != "" || !fetchData) {
                        // ------ THE TITLE ------

                        try {
                            // ----- GET -----
                            if (fetchData) {
                                int seasons = 0; // d.Count<string>("");

                                for (int i = 1; i <= 100; i++) {
                                    if (d.Contains("episodes?season=" + i)) {
                                        seasons = i;
                                    }
                                }
                                string result = FindHTML(d, "<div class=\"title_wrapper\">", "</a>            </div>");
                                string descript = FindHTML(d, "<div class=\"summary_text\">", "<").Replace("\n", "").Replace("  ", " ").Replace("          ", ""); // string descript = FindHTML(d, "\"description\": \"", "\"");
                                if (descript == "") {
                                    descript = FindHTML(d, "\"description\": \"", "\"", decodeToNonHtml: true);
                                }
                                // print("Dscript: " + descript);
                                string __d = RemoveOne(d, "<div class=\"poster\">");
                                string hdPosterUrl = FindHTML(__d, "src=\"", "\"");
                                string ogName = FindHTML(d, "\"name\": \"", "\"", decodeToNonHtml: true);
                                string rating = FindHTML(d, "\"ratingValue\": \"", "\"");
                                string posterUrl = FindHTML(d, "\"image\": \"", "\"");
                                string genres = FindHTML(d, "\"genre\": [", "]");
                                string type = FindHTML(d, "@type\": \"", "\"");
                                string _trailer = FindHTML(d, "\"trailer\": ", "uploadDate");
                                string trailerUrl = "https://imdb.com" + FindHTML(_trailer, "\"embedUrl\": \"", "\"");
                                string trailerImg = FindHTML(_trailer, "\"thumbnailUrl\": \"", "\"");
                                string trailerName = FindHTML(_trailer, "\"name\": \"", "\"");
                                string keyWord = FindHTML(d, "\"keywords\": \"", "\"");
                                string duration = FindHTML(d, "<time datetime=\"PT", "\"").Replace("M", "min");
                                string year = FindHTML(d, "datePublished\": \"", "-");

                                //<span class="bp_sub_heading">66 episodes</span> //total episodes

                                List<string> allGenres = new List<string>();
                                int counter = 0;
                                while (genres.Contains("\"") && counter < 20) {
                                    counter++;
                                    string genre = FindHTML(genres, "\"", "\"");
                                    allGenres.Add(genre);
                                    genres = genres.Replace("\"" + genre + "\"", "");
                                }

                                MovieType movieType = (!keyWords.Contains("anime") ? (type == "Movie" ? MovieType.Movie : MovieType.TVSeries) : (type == "Movie" ? MovieType.AnimeMovie : MovieType.Anime)); // looks ugly but works

                                if (movieType == MovieType.TVSeries) { // JUST IN CASE
                                    if (d.Contains(">Japan</a>") && d.Contains(">Japanese</a>") && (d.Contains("Anime") || d.Contains(">Animation</a>,"))) {
                                        movieType = MovieType.Anime;
                                    }
                                }

                                // ----- SET -----
                                activeMovie.title = new Title() {
                                    name = REPLACE_IMDBNAME_WITH_POSTERNAME ? imdb.name : ogName,
                                    posterUrl = posterUrl,
                                    trailers = new List<Trailer>(),
                                    rating = rating,
                                    genres = allGenres,
                                    id = imdb.url.Replace("https://imdb.com/title/", ""),
                                    description = descript,
                                    runtime = duration,
                                    seasons = seasons,
                                    MALData = new MALData() { japName = "", seasonData = new List<MALSeasonData>(), currentSelectedYear = "" },
                                    movieType = movieType,
                                    year = year,
                                    ogName = ogName,
                                    hdPosterUrl = hdPosterUrl,
                                    fmoviesMetaData = new List<FMoviesData>(),
                                    watchSeriesHdMetaData = new List<WatchSeriesHdMetaData>(),

                                };

                                activeMovie.title.trailers.Add(new Trailer() { Url = trailerUrl, PosterUrl = trailerImg, Name = trailerName });

                            }
                            try {
                                if (autoSearchTrailer) { GetRealTrailerLinkFromImdb(true); }
                            }
                            catch (Exception) {

                            }

                            if (activeMovie.title.movieType == MovieType.Anime) {
                                GetMALData();
                            }
                            else { // FISHING : THIS IS TO SPEED UP LINK FETHING
                                FishFmovies();
                                FishMovies123Links();
                                FishYesMoviesLinks();
                                FishWatchSeries();
                            }

                        }
                        catch (Exception) { }

                        // ------ RECOMENDATIONS ------

                        if (fetchData) {
                            activeMovie.title.recomended = new List<Poster>();
                            string lookFor = "<div class=\"rec_item\" data-info=\"\" data-spec=\"";
                            for (int i = 0; i < 12; i++) {
                                try {
                                    string result = FindHTML(d, lookFor, "/> <br/>");
                                    string id = FindHTML(result, "data-tconst=\"", "\"");
                                    string name = FindHTML(result, "title=\"", "\"", decodeToNonHtml: true);
                                    string posterUrl = FindHTML(result, "loadlate=\"", "\"");

                                    d = RemoveOne(d, result);
                                    Poster p = new Poster() { url = id, name = name, posterUrl = posterUrl, posterType = PosterType.Imdb };

                                    // if (!activeMovie.title.recomended.Contains(p)) {
                                    activeMovie.title.recomended.Add(p);
                                    // }

                                }
                                catch (Exception) {

                                }
                            }
                            if (cacheData && Settings.CacheImdb) {
                                App.SetKey("CacheImdb", __id, activeMovie);
                            }
                        }
                        titleLoaded?.Invoke(null, activeMovie);
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Imdb Recomended";
            tempThred.Thread.Start();
        }

        public static void MonitorFunc(Action a, int sleepTime = 100)
        {
            Thread t = new Thread(() => {

                while (true) {
                    a();
                    Thread.Sleep(sleepTime);
                }

            }) {
                Name = "MonitorFunc"
            };
            t.Start();
        }

        public static void FishMovies123Links() // TO MAKE LINK EXTRACTION EASIER
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    if (activeMovie.title.movieType == MovieType.Anime) { return; }

                    bool canMovie = GetSettings(MovieType.Movie);
                    bool canShow = GetSettings(MovieType.TVSeries);

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                    string yesmovies = "https://yesmoviess.to/search/?keyword=" + rinput.Replace("+", "-");

                    // SUB HD MOVIES 123
                    string movies123 = "https://movies123.pro/search/" + rinput.Replace("+", "%20") + ((activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie) ? "/movies" : "/series");

                    string d = DownloadString(movies123, tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    int counter = 0; // NOT TO GET STUCK, JUST IN CASE

                    List<Movies123SeasonData> seasonData = new List<Movies123SeasonData>();

                    while ((d.Contains("/movie/") || d.Contains("/tv-series/")) && counter < 100) {
                        counter++;

                        /*
                        data - filmName = "Iron Man"
                    data - year = "2008"
                    data - imdb = "IMDb: 7.9"
                    data - duration = "126 min"
                    data - country = "United States"
                    data - genre = "Action, Adventure, Sci-Fi"
                    data - descript = "Tony a boss of a Technology group, after his encounter in Afghanistan, became a symbol of justice as he built High-Tech armors and suits, to act as..."
                    data - star_prefix = ""
                    data - key = "0"
                    data - quality = "itemAbsolute_hd"
                    data - rating = "4.75"
                            */

                        // --------- GET TYPE ---------

                        int tvIndex = d.IndexOf("/tv-series/");
                        int movieIndex = d.IndexOf("/movie/");
                        bool isMovie = movieIndex < tvIndex;
                        if (tvIndex == -1) { isMovie = true; }
                        if (movieIndex == -1) { isMovie = false; }

                        Movies123 movie123 = new Movies123();

                        // --------- GET CROSSREFRENCE DATA ---------

                        movie123.year = ReadDataMovie(d, "data-year");
                        movie123.imdbRating = ReadDataMovie(d, "data-imdb").ToLower().Replace(" ", "").Replace("imdb:", "");
                        movie123.runtime = ReadDataMovie(d, "data-duration").Replace(" ", "");
                        movie123.genre = ReadDataMovie(d, "data-genre");
                        movie123.plot = ReadDataMovie(d, "data-descript");
                        movie123.type = isMovie ? MovieType.Movie : MovieType.TVSeries; //  "movie" : "tv-series";

                        string lookfor = isMovie ? "/movie/" : "/tv-series/";

                        // --------- GET FWORLDLINK, FORWARLINK ---------

                        int mStart = d.IndexOf(lookfor);
                        if (mStart == -1) {
                            debug("API ERROR!");
                            // print(mD);
                            debug(movie123.year + "|" + movie123.imdbRating + "|" + isMovie + "|" + lookfor);
                            continue;
                        }
                        d = d.Substring(mStart, d.Length - mStart);
                        d = d.Substring(7, d.Length - 7);
                        //string bMd = RemoveOne(mD, "<img src=\"/dist/image/default_poster.jpg\"");
                        movie123.posterUrl = ReadDataMovie(d, "<img src=\"/dist/image/default_poster.jpg\" data-src");




                        string rmd = lookfor + d;
                        //string realAPILink = mD.Substring(0, mD.IndexOf("-"));
                        string fwordLink = "https://movies123.pro" + rmd.Substring(0, rmd.IndexOf("\""));
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        if (!isMovie) {
                            fwordLink = rmd.Substring(0, rmd.IndexOf("\"")); // /tv-series/ies/the-orville-season-2/gMSTqyRs
                            fwordLink = fwordLink.Substring(11, fwordLink.Length - 11); //ies/the-orville-season-2/gMSTqyRs
                            string found = fwordLink.Substring(0, fwordLink.IndexOf("/"));
                            if (!found.Contains("-")) {
                                fwordLink = fwordLink.Replace(found, ""); //the-orville-season-2/gMSTqyRs
                            }
                            fwordLink = "https://movies123.pro" + "/tv-series" + fwordLink;
                        }

                        // --------- GET NAME ECT ---------
                        //if (false) {
                        int titleStart = d.IndexOf("title=\"");
                        string movieName = d.Substring(titleStart + 7, d.Length - titleStart - 7);
                        movieName = movieName.Substring(0, movieName.IndexOf("\""));
                        movieName = movieName.Replace("&amp;", "and");
                        movie123.name = movieName;
                        //}

                        if ((isMovie && canMovie) || (!isMovie && canShow)) {
                            //FWORDLINK HERE
                            //   print(activeMovie.title.name + "||||" + movie123.name + " : " + activeMovie.title.rating + " : " + movie123.imdbRating + " : " + activeMovie.title.movieType + " : " + movie123.type + " : " + activeMovie.title.runtime + " : " + movie123.runtime);

                            // GET RATING IN INT (10-100)
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string s1 = activeMovie.title.rating;
                            string s2 = movie123.imdbRating;
                            if (s2.ToLower() == "n/a") {
                                continue;
                            }

                            if (!s1.Contains(".")) { s1 += ".0"; }
                            if (!s2.Contains(".")) { s2 += ".0"; }

                            int i1 = int.Parse(s1.Replace(".", ""));
                            int i2 = int.Parse(s2.Replace(".", ""));

                            if ((i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1) && activeMovie.title.movieType == movie123.type && movie123.name.ToLower().Contains(activeMovie.title.name.ToLower())) { // --- THE SAME ---
                                                                                                                                                                                                    // counter = 10000;
                                                                                                                                                                                                    //print("FWORDLINK: " + fwordLink);
                                if (activeMovie.title.movieType == MovieType.TVSeries) {
                                    //<a data-ep-id="
                                    string _d = DownloadString(fwordLink, tempThred);
                                    string _lookFor = "<a data-ep-id=\"";
                                    //print(_d);
                                    List<string> sData = new List<string>();
                                    while (_d.Contains(_lookFor)) {
                                        string rLink = FindHTML(_d, _lookFor, "\"");
                                        //   print("RLINK: " + rLink);
                                        sData.Add(rLink + "-watch-free.html");
                                        _d = RemoveOne(_d, _lookFor);
                                    }
                                    seasonData.Add(new Movies123SeasonData() { seasonUrl = fwordLink, episodeUrls = sData });
                                }
                                else {
                                    activeMovie.title.movies123MetaData = new Movies123MetaData() { movieLink = fwordLink, seasonData = new List<Movies123SeasonData>() };
                                }

                            }
                        }
                    }

                    seasonData.Reverse();
                    if (MovieType.TVSeries == activeMovie.title.movieType) {
                        Title t = activeMovie.title;
                        activeMovie.title = new Title() {
                            description = t.description,
                            MALData = t.MALData,
                            genres = t.genres,
                            id = t.id,
                            movieType = t.movieType,
                            name = t.name,
                            ogName = t.ogName,
                            posterUrl = t.posterUrl,
                            rating = t.rating,
                            recomended = t.recomended,
                            runtime = t.runtime,
                            seasons = t.seasons,
                            trailers = t.trailers,
                            year = t.year,
                            hdPosterUrl = t.hdPosterUrl,
                            shortEpView = t.shortEpView,
                            fmoviesMetaData = t.fmoviesMetaData,
                            movies123MetaData = new Movies123MetaData() { movieLink = "", seasonData = seasonData },
                            yesmoviessSeasonDatas = t.yesmoviessSeasonDatas,
                            watchSeriesHdMetaData = t.watchSeriesHdMetaData,
                        };
                    }

                    movie123FishingDone?.Invoke(null, activeMovie);
                    fishingDone?.Invoke(null, activeMovie);

                    // MonitorFunc(() => print(">>>" + activeMovie.title.movies123MetaData.seasonData.Count),0);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Movies123MetaData";
            tempThred.Thread.Start();
        }

        public static void FishFmovies()
        {
            if (!FMOVIES_ENABLED) return;

            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    if (activeMovie.title.movieType == MovieType.Anime) { return; }

                    bool canMovie = GetSettings(MovieType.Movie);
                    bool canShow = GetSettings(MovieType.TVSeries);

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                    string url = "https://fmovies.to/search?keyword=" + rinput.Replace("+", "%20");
                    string realName = activeMovie.title.name;
                    bool isMovie = (activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie);
                    string realYear = activeMovie.title.year;

                    List<FMoviesData> data = new List<FMoviesData>();

                    string d = HTMLGet(url, "https://fmovies.to");
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    string lookFor = "class=\"name\" href=\"/film/";
                    while (d.Contains(lookFor)) {
                        string _url = FindHTML(d, lookFor, "\"");
                        //print(_url);
                        string ajax = FindHTML(d, "data-tip=\"ajax/film/", "\"");

                        d = RemoveOne(d, lookFor);
                        string name = FindHTML(d, ">", "<");

                        bool same = false;
                        int season = 0;
                        same = name.Replace(" ", "").ToLower() == realName.Replace(" ", "").ToLower();
                        if (!same && !isMovie) {
                            for (int i = 1; i < 100; i++) {
                                if (name.Replace(" ", "").ToLower() == realName.Replace(" ", "").ToLower() + i) {
                                    same = true;
                                    season = i;
                                    break;
                                }
                            }
                        }

                        //  var result = Regex.Replace(name, @"[0-9\-]", string.Empty);

                        bool isSame = false;
                        if (same) {
                            if (isMovie) {
                                string ajaxDownload = DownloadString("https://fmovies.to/ajax/film/" + ajax);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                if (ajaxDownload == "") {
                                    print("AJAX");
                                }
                                else {
                                    string ajaxYear = FindHTML(ajaxDownload, "<span>", "<");
                                    string ajaxIMDb = FindHTML(ajaxDownload, "<i>IMDb</i> ", "<"); // 9.0 = 9
                                    if (ajaxYear == realYear) {
                                        isSame = true;
                                    }
                                }
                            }
                            else {
                                isSame = true;
                            }
                        }
                        if (isSame) {
                            data.Add(new FMoviesData() { url = _url, season = season });
                            print(name + "|" + _url + "|" + season);
                        }

                        // print(ajaxDownload);
                    }
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    activeMovie.title.fmoviesMetaData = data;
                    fmoviesFishingDone?.Invoke(null, activeMovie);
                    fishingDone?.Invoke(null, activeMovie);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "FishFmovies";
            tempThred.Thread.Start();
        }

        static void FishWatchSeries()
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    if (activeMovie.title.movieType == MovieType.Anime) { return; }

                    bool canMovie = GetSettings(MovieType.Movie);
                    bool canShow = GetSettings(MovieType.TVSeries);

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                    string url = "http://watchserieshd.tv/search.html?keyword=" + rinput.Replace("+", "%20");

                    string d = DownloadString(url);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string lookFor = " <div class=\"vid_info\">";
                    List<FishWatch> fishWatches = new List<FishWatch>();

                    while (d.Contains(lookFor)) {
                        d = RemoveOne(d, lookFor);
                        string href = FindHTML(d, "<a href=\"", "\"");
                        if (href.Contains("/drama-info")) continue;
                        string title = FindHTML(d, "title=\"", "\"");
                        string _d = DownloadString("http://watchserieshd.tv" + href);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        try {
                            string imdbScore = FindHTML(_d, "IMDB: ", " ");
                            string released = FindHTML(_d, "Released: ", " ").Substring(0, 4);
                            int season = -1;
                            for (int i = 0; i < 100; i++) {
                                if (title.Contains(" - Season " + i)) {
                                    season = i;
                                }
                            }
                            string removedTitle = title.Replace(" - Season " + season, "").Replace(" ", "");

                            print(imdbScore + "|" + released + "|" + href + "|" + title + "|" + removedTitle);

                            fishWatches.Add(new FishWatch() { imdbScore = imdbScore, released = released, removedTitle = removedTitle, season = season, title = title, href = href });
                        }
                        catch (Exception) {

                        }


                        // MonitorFunc(() => print(">>>" + activeMovie.title.movies123MetaData.seasonData.Count),0);
                    }

                    List<FishWatch> nonSeasonOne = new List<FishWatch>();
                    List<FishWatch> seasonOne = new List<FishWatch>();
                    List<FishWatch> other = new List<FishWatch>();
                    for (int i = 0; i < fishWatches.Count; i++) {

                        if (fishWatches[i].season > 1) {
                            nonSeasonOne.Add(fishWatches[i]);
                        }
                        else if (fishWatches[i].season == 1) {
                            seasonOne.Add(fishWatches[i]);
                            other.Add(fishWatches[i]);
                        }
                        else {
                            other.Add(fishWatches[i]);
                        }

                    }
                    for (int q = 0; q < nonSeasonOne.Count; q++) {
                        for (int z = 0; z < seasonOne.Count; z++) {
                            if (nonSeasonOne[q].removedTitle == seasonOne[z].removedTitle) {
                                FishWatch f = nonSeasonOne[q];
                                f.released = seasonOne[z].released;
                                other.Add(f);
                            }
                        }
                    }
                    activeMovie.title.watchSeriesHdMetaData = new List<WatchSeriesHdMetaData>();
                    other = other.OrderBy(t => t.season).ToList();
                    for (int i = 0; i < other.Count; i++) {
                        string s1 = activeMovie.title.rating;
                        string s2 = other[i].imdbScore;
                        if (s2.ToLower() == "n/a") {
                            continue;
                        }

                        if (!s1.Contains(".")) { s1 += ".0"; }
                        if (!s2.Contains(".")) { s2 += ".0"; }

                        int i1 = int.Parse(s1.Replace(".", ""));
                        int i2 = int.Parse(s2.Replace(".", ""));

                        print(i1 + "||" + i2 + "START:::" + ToDown(other[i].removedTitle.Replace("-", "").Replace(":", ""), replaceSpace: "") + "<<>>" + ToDown(activeMovie.title.name.Replace("-", "").Replace(":", ""), replaceSpace: "") + ":::");
                        if ((i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1) && ToDown(other[i].removedTitle.Replace("-", "").Replace(":", ""), replaceSpace: "") == ToDown(activeMovie.title.name.Replace("-", "").Replace(":", ""), replaceSpace: "")) {

                            if (other[i].released == activeMovie.title.ogYear || activeMovie.title.movieType != MovieType.Movie) {
                                print("TRUE:::::" + other[i].imdbScore + "|" + other[i].released + "|" + other[i].href + "|" + other[i].title + "|" + other[i].removedTitle);
                                if (other[i].href != "") {
                                    activeMovie.title.watchSeriesHdMetaData.Add(new WatchSeriesHdMetaData() { season = other[i].season, url = other[i].href });
                                }
                            }
                        }
                    }
                    watchSeriesFishingDone?.Invoke(null, activeMovie);
                    fishingDone?.Invoke(null, activeMovie);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "WatchSeriesHdMetaData";
            tempThred.Thread.Start();
        }

        // DONT USE  https://www1.moviesjoy.net/search/ THEY USE GOOGLE RECAPTCH TO GET LINKS
        // DONT USE https://gostream.site/iron-man/ THEY HAVE DDOS PROTECTION

        static void GetLinksFromWatchSeries(int season, int normalEpisode)
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    if (activeMovie.title.watchSeriesHdMetaData.Count == 1) {
                        season = activeMovie.title.watchSeriesHdMetaData[0].season;
                    }
                    for (int i = 0; i < activeMovie.title.watchSeriesHdMetaData.Count; i++) {
                        var meta = activeMovie.title.watchSeriesHdMetaData[i];
                        if (meta.season == season) {
                            string href = "http://watchserieshd.tv" + meta.url + "-episode-" + (normalEpisode + 1);
                            string d = DownloadString(href, tempThred);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            string dError = "<h1 class=\"entry-title\">Page not found</h1>";
                            if (d.Contains(dError) && activeMovie.title.movieType == MovieType.Movie) {
                                href = "http://watchserieshd.tv" + meta.url + "-episode-0";
                                d = DownloadString(href, tempThred);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            }
                            if (d.Contains(dError)) {

                            }
                            else {

                                AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                            }
                            print("HREF:" + href);
                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "GetLinksFromWatchSeries";
            tempThred.Thread.Start();
        }

        public static void FishYesMoviesLinks() // TO MAKE LINK EXTRACTION EASIER, http://vumoo.to/
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    if (activeMovie.title.movieType == MovieType.Anime) { return; }

                    bool canMovie = GetSettings(MovieType.Movie);
                    bool canShow = GetSettings(MovieType.TVSeries);

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                    string yesmovies = "https://yesmoviess.to/search/?keyword=" + rinput.Replace("+", "-");


                    string d = DownloadString(yesmovies, tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    int counter = 0;
                    string lookfor = "data-url=\"";
                    while ((d.Contains(lookfor)) && counter < 100) {
                        counter++;
                        string url = FindHTML(d, lookfor, "\"");
                        string remove = "class=\"ml-mask jt\" title=\"";
                        string title = FindHTML(d, remove, "\"");
                        string movieUrl = "https://yesmoviess.to/movie/" + FindHTML(d, "<a href=\"https://yesmoviess.to/movie/", "\"");
                        d = RemoveOne(d, remove);

                        int seasonData = 1;
                        for (int i = 0; i < 100; i++) {
                            if (title.Contains(" - Season " + i)) {
                                seasonData = i;
                            }
                        }
                        string realtitle = title.Replace(" - Season " + seasonData, "");
                        string _d = DownloadString(url, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        string imdbData = FindHTML(_d, "IMDb: ", "<").Replace("\n", "").Replace(" ", "").Replace("	", "");
                        //  string year = FindHTML(_d, "<div class=\"jt-info\">", "<").Replace("\n", "").Replace(" ", "").Replace("	", "").Replace("	", "");

                        string s1 = activeMovie.title.rating;
                        string s2 = imdbData;
                        if (s2.ToLower() == "n/a") {
                            continue;
                        }

                        if (!s1.Contains(".")) { s1 += ".0"; }
                        if (!s2.Contains(".")) { s2 += ".0"; }

                        int i1 = int.Parse(s1.Replace(".", ""));
                        int i2 = int.Parse(s2.Replace(".", ""));
                        //activeMovie.title.year.Substring(0, 4) == year
                        if (ToDown(activeMovie.title.name, replaceSpace: "") == ToDown(realtitle, replaceSpace: "") && (i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1)) {
                            print("TRUE: " + imdbData + "|" + realtitle);
                            if (activeMovie.title.yesmoviessSeasonDatas == null) {
                                activeMovie.title.yesmoviessSeasonDatas = new List<YesmoviessSeasonData>();
                            }
                            activeMovie.title.yesmoviessSeasonDatas.Add(new YesmoviessSeasonData() { url = movieUrl, id = seasonData });
                        }
                        print("DATA:" + imdbData + "|" + movieUrl + "|" + realtitle + "|" + title + "|" + seasonData + "|" + url + "|" + i1 + "|" + i2);
                    }
                    yesmovieFishingDone?.Invoke(null, activeMovie);
                    fishingDone?.Invoke(null, activeMovie);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "YesMoviesMetaData";
            tempThred.Thread.Start();
        }
        public static void GetRealTrailerLinkFromImdbSingle(string url, int index, TempThred tempThred) // LOOK AT https://www.imdb.com/title/tt4508902/trailers/;; ///video/imdb/vi3474439449
        {
            url = url.Replace("video/imdb", "videoplayer");
            string d = GetHTML(url);
            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
            string key = FindHTML(d, "playbackDataKey\":[\"", "\"");
            d = GetHTML("https://www.imdb.com/ve/data/VIDEO_PLAYBACK_DATA?key=" + key);
            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

            string realURL = FindHTML(d, "\"video/mp4\",\"url\":\"", "\"");
            try {
                Trailer t = activeMovie.title.trailers[index];
                activeMovie.title.trailers[index] = new Trailer() { Name = t.Name, PosterUrl = t.PosterUrl, Url = realURL };

            }
            catch (Exception) {
                return;
            }
        }


        public static void GetRealTrailerLinkFromImdb(bool purgeCurrentTrailerThread = false) // LOOK AT https://www.imdb.com/title/tt4508902/trailers/;; ///video/imdb/vi3474439449
        {
            if (purgeCurrentTrailerThread) {
                PurgeThreds(5);
            }
            TempThred tempThred = new TempThred();
            tempThred.typeId = 5; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {

                    string d = DownloadString("https://www.imdb.com/title/" + activeMovie.title.id + "/trailers/");
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    print(d);
                    string lookfor = "viconst=\"";
                    int index = 0;
                    while (d.Contains(lookfor)) {
                        string viUrl = FindHTML(d, lookfor, "\"");
                        string poster = FindHTML(d, "loadlate=\"", "\"");
                        string rep = FindHTML(poster, "._", "_.");
                        poster = poster.Replace("._" + rep + "_", "._V1_UY1000_UX1000_AL_");

                        d = RemoveOne(d, lookfor);
                        string name = FindHTML(d, "class=\"video-modal\" >", "<", decodeToNonHtml: true);
                        var cT = new Trailer() { Name = name, PosterUrl = poster, Url = "" };
                        if (activeMovie.title.trailers == null) return;
                        if (activeMovie.title.trailers.Count > index) {
                            activeMovie.title.trailers[index] = cT;
                        }
                        else {
                            activeMovie.title.trailers.Add(cT);
                        }

                        GetRealTrailerLinkFromImdbSingle("https://imdb.com/video/imdb/" + viUrl, index, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        print("TRAILER::" + viUrl + "|" + name + "|" + poster);

                        index++;
                        trailerLoaded?.Invoke(null, activeMovie.title.trailers);

                        print(viUrl + "|" + name);
                    }

                    /*
                    url = url.Replace("video/imdb", "videoplayer");
                    string d = GetHTML(url);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string realTrailerUrl = FindHTML(d, "videoUrl\":\"", "\"");

                    for (int i = 0; i < 10; i++) {
                        realTrailerUrl = RemoveOne(realTrailerUrl, "\\u002F");
                    }
                    try {
                        realTrailerUrl = realTrailerUrl.Substring(5, realTrailerUrl.Length - 5);
                        realTrailerUrl = ("https://imdb-video.media-imdb.com/" + (url.Substring(url.IndexOf("/vi") + 1, url.Length - url.IndexOf("/vi") - 1)) + "/" + realTrailerUrl).Replace("/videoplayer", "");
                        activeTrailer = realTrailerUrl;
                        trailerLoaded?.Invoke(null, realTrailerUrl);
                    }
                    catch (Exception) {

                    }*/

                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Trailer";
            tempThred.Thread.Start();
        }

        public static void GetImdbEpisodes(int season = 1, bool purgeCurrentSeasonThread = true)
        {
            if (purgeCurrentSeasonThread) {
                PurgeThreds(6);
            }
            if (activeMovie.title.movieType == MovieType.Anime || activeMovie.title.movieType == MovieType.TVSeries) {
                TempThred tempThred = new TempThred();
                tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        string url = "https://www.imdb.com/title/" + activeMovie.title.id + "/episodes?season=" + season;
                        string d = DownloadString(url, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        int eps = 0;

                        for (int q = 0; q < 1000; q++) {
                            if (d.Contains("?ref_=ttep_ep" + q)) {
                                eps = q;
                            }
                        }
                        if (activeMovie.title.movieType == MovieType.Anime) {
                            while (!activeMovie.title.MALData.done) {
                                Thread.Sleep(100);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            }
                            //string _d = DownloadString("");
                        }

                        activeMovie.episodes = new List<Episode>();

                        for (int q = 1; q <= eps; q++) {
                            string lookFor = "?ref_=ttep_ep" + q;
                            try {
                                d = d.Substring(d.IndexOf(lookFor), d.Length - d.IndexOf(lookFor));
                                string name = FindHTML(d, "title=\"", "\"");
                                string id = FindHTML(d, "div data-const=\"", "\"");
                                string rating = FindHTML(d, "<span class=\"ipl-rating-star__rating\">", "<");
                                string descript = FindHTML(d, "<div class=\"item_description\" itemprop=\"description\">", "<").Replace("\n", "").Replace("  ", "");
                                string date = FindHTML(d, "<div class=\"airdate\">", "<").Replace("\n", "").Replace("  ", "");
                                string posterUrl = FindHTML(d, "src=\"", "\"");

                                if (posterUrl == "https://m.media-amazon.com/images/G/01/IMDb/spinning-progress.gif" || posterUrl.Replace(" ", "") == "") {
                                    posterUrl = loadingImage; // DEAFULT LOADING
                                }

                                if (descript == "Know what this is about?") {
                                    descript = "";
                                }
                                activeMovie.episodes.Add(new Episode() { date = date, name = name, description = descript, rating = rating, posterUrl = posterUrl, id = id });

                            }
                            catch (Exception) {

                            }
                        }
                        //print(activeMovie.title.MALData.japName + "<<<<<<<<<<<<<<<<<<<<<<<<");
                        //     https://www9.gogoanime.io/category/mix-meisei-story

                        episodeLoaded?.Invoke(null, activeMovie.episodes);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "Season Info";
                tempThred.Thread.Start();
            }
            else {
                Episode ep = new Episode() { name = activeMovie.title.name };
                activeMovie.episodes = new List<Episode>();
                activeMovie.episodes.Add(ep);
                episodeLoaded?.Invoke(null, activeMovie.episodes);
            }
        }

        /// <summary>
        /// RETURN SUBTITLE STRING
        /// </summary>
        /// <param name="imdbTitleId"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string DownloadSubtitle(string imdbTitleId, string lang = "eng", bool showToast = true)
        {
            try {
                string rUrl = "https://www.opensubtitles.org/en/search/sublanguageid-" + lang + "/imdbid-" + imdbTitleId + "/sort-7/asc-0"; // best match first
                                                                                                                                            //print(rUrl);
                string d = DownloadString(rUrl);
                if (d.Contains("<div class=\"msg warn\"><b>No results</b> found, try")) {
                    return "";
                }
                string _url = "https://www.opensubtitles.org/" + lang + "/subtitles/" + FindHTML(d, "en/subtitles/", "\'");

                d = DownloadString(_url);
                const string subAdd = "https://dl.opensubtitles.org/en/download/file/";
                string subtitleUrl = subAdd + FindHTML(d, "download/file/", "\"");
                if (subtitleUrl != subAdd) {
                    string s = HTMLGet(subtitleUrl, "https://www.opensubtitles.org");
                    if (BAN_SUBTITLE_ADS) {
                        List<string> bannedLines = new List<string>() { "Support us and become VIP member", "to remove all ads from www.OpenSubtitles.org", "to remove all ads from OpenSubtitles.org", "Advertise your product or brand here", "contact www.OpenSubtitles.org today" }; // No advertisement
                        foreach (var banned in bannedLines) {
                            s = s.Replace(banned, "");
                        }
                    }
                    s = s.Replace("\n\n", "");
                    if (showToast) {
                        App.ShowToast("Subtitles Downloaded");
                    }
                    return s;
                }
                else {
                    return "";
                }
            }
            catch (Exception) {
                return "";
            }
        }

        public static int GetMaxEpisodesInAnimeSeason(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred = null)
        {
            int max = 0;
            int maxGogo = 0;
            int maxDubbed = 0;

            List<int> saved = new List<int>();
            //activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason = new List<int>();
            List<string> baseUrls = GetAllGogoLinksFromAnime(currentMovie, currentSeason, isDub);
            if (baseUrls.Count > 0) {
                for (int i = 0; i < baseUrls.Count; i++) {
                    string dstring = baseUrls[i];
                    dstring = dstring.Replace("-dub", "") + (isDub ? "-dub" : "");
                    string d = DownloadString("https://www9.gogoanime.io/category/" + dstring);
                    if (d != "") {
                        if (tempThred != null) {
                            if (!GetThredActive((TempThred)tempThred)) { return max; }; // COPY UPDATE PROGRESS
                        }
                        string subMax = FindHTML(d, "class=\"active\" ep_start = \'", ">");
                        string maxEp = FindHTML(subMax, "ep_end = \'", "\'");//FindHTML(d, "<a href=\"#\" class=\"active\" ep_start = \'0\' ep_end = \'", "\'");
                        print(i + "MAXEP" + maxEp);
                        print(baseUrls[i]);
                        int _epCount = (int)Math.Floor(decimal.Parse(maxEp));
                        //max += _epCount;
                        try {
                            saved.Add(_epCount);
                        }
                        catch (Exception) {

                        }
                    }
                }
                maxGogo = saved.Sum();
                activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason = saved;

            }

            if (isDub) {
                //  activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason = new List<int>();
                List<int> dubbedSum = new List<int>();
                List<string> dubbedAnimeLinks = GetAllDubbedAnimeLinks(currentMovie, currentSeason);
                if (tempThred != null) {
                    if (!GetThredActive((TempThred)tempThred)) { return max; }; // COPY UPDATE PROGRESS
                }
                for (int i = 0; i < dubbedAnimeLinks.Count; i++) {
                    print("LINKOS:" + dubbedAnimeLinks[i]);
                    DubbedAnimeEpisode ep = GetDubbedAnimeEpisode(dubbedAnimeLinks[i], 1);
                    print("EPOS:" + ep.totalEp);
                    dubbedSum.Add(ep.totalEp);
                    // activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Add(ep.totalEp);
                }
                maxDubbed = dubbedSum.Sum();
                activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason = dubbedSum;
            }
            print("MAX:" + maxDubbed + "|" + maxGogo);
            max = Math.Max(maxDubbed, maxGogo);
            return max;
        }

        public static List<string> GetAllDubbedAnimeLinks(Movie currentMovie, int currentSeason)
        {
            List<string> baseUrls = new List<string>();

            try {
                for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                    var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].dubbedAnimeData;

                    if (ms.dubExists) {
                        if (!baseUrls.Contains(ms.slug)) {
                            baseUrls.Add(ms.slug);
                        }
                        //print("BASEURL " + ms.baseUrl);
                    }
                }
            }
            catch (Exception) {
                //  throw;
            }
            return baseUrls;
        }

        public static List<string> GetAllGogoLinksFromAnime(Movie currentMovie, int currentSeason, bool isDub)
        {
            List<string> baseUrls = new List<string>();

            try {
                for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                    var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].gogoData;

                    if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                        //  dstring = ms.baseUrl;
                        string burl = isDub ? ms.dubUrl : ms.subUrl;
                        if (!baseUrls.Contains(burl)) {
                            baseUrls.Add(burl);
                        }
                        //print("BASEURL " + ms.baseUrl);
                    }
                }
            }
            catch (Exception) {
            }
            return baseUrls;
        }

        public static void DownloadSubtitlesAndAdd(string lang = "eng", bool isEpisode = false, int episodeCounter = 0)
        {
            if (!globalSubtitlesEnabled) { return; }

            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string id = activeMovie.title.id;
                    if (isEpisode) {
                        id = activeMovie.episodes[episodeCounter].id;
                    }

                    string _subtitleLoc = DownloadSubtitle(id, lang);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    bool contains = false;
                    if (activeMovie.subtitles == null) {
                        activeMovie.subtitles = new List<Subtitle>();
                    }

                    for (int i = 0; i < activeMovie.subtitles.Count; i++) {
                        if (activeMovie.subtitles[i].name == lang) {
                            contains = true;
                        }
                    }
                    if (!contains) {
                        activeMovie.subtitles.Add(new Subtitle() { name = lang, data = _subtitleLoc });
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "SubtitleThread";
            tempThred.Thread.Start();
        }

        static bool LookForFembedInString(TempThred tempThred, int normalEpisode, string d)
        {
            string source = "https://www.fembed.com";
            string _ref = "www.fembed.com";

            print("FMEMEDOSOOS:" + d);



            string fembed = FindHTML(d, "data-video=\"https://www.fembed.com/v/", "\"");
            if (fembed == "") {
                fembed = FindHTML(d, "data-video=\"https://gcloud.live/v/", "\"");
                if (fembed != "") {
                    source = "https://gcloud.live";
                    _ref = "www.gcloud.live";
                }
            }
            if (fembed != "") {
                GetFembed(fembed, tempThred, normalEpisode, source, _ref);
            }
            string lookFor = "file: \'";
            int prio = 5;
            while (d.Contains(lookFor)) {
                string ur = FindHTML(d, lookFor, "\'");
                d = RemoveOne(d, lookFor);
                string label = FindHTML(d, "label: \'", "\'").Replace("hls P", "live").Replace(" P", "p");
                prio--;
                AddPotentialLink(normalEpisode, ur, "Fembed " + label, prio);
            }
            return fembed != "";
        }

        static int Random(int min, int max)
        {
            return rng.Next(min, max);
        }

        static void GetFmoviesLinks(int normlaEpisode, int episode, int season)
        {
            if (!FMOVIES_ENABLED) return;

            TempThred tempThred = new TempThred();

            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    print("FMOVIESMETA:" + activeMovie.title.fmoviesMetaData);

                    if (activeMovie.title.fmoviesMetaData == null) return;
                    bool isMovie = (activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie);
                    string url = "";
                    for (int i = 0; i < activeMovie.title.fmoviesMetaData.Count; i++) {
                        if (activeMovie.title.fmoviesMetaData[i].season == season || isMovie) {
                            url = activeMovie.title.fmoviesMetaData[i].url;
                            break;
                        }
                    }
                    print("FMOVIESURL:" + url);

                    if (url == "") return;

                    string d = HTMLGet("https://fmovies.to/film/" + url, "https://fmovies.to");
                    string dataTs = FindHTML(d, "data-ts=\"", "\"");
                    string dataId = FindHTML(d, "data-id=\"", "\"");
                    string dataEpId = FindHTML(d, "data-epid=\"", "\"");
                    string _url = "https://fmovies.to/ajax/film/servers/" + dataId + "?episode=" + dataEpId + "&ts=" + dataTs + "&_=" + Random(100, 999); //
                    print(_url);
                    //d = DownloadString(_url);
                    d = HTMLGet(_url, "https://fmovies.to");

                    print(d);

                    string cloudGet = "";
                    string cLookFor = "<a  data-id=\\\"";
                    while (d.Contains(cLookFor)) {
                        string _cloudGet = FindHTML(d, cLookFor, "\\\"");
                        d = RemoveOne(d, cLookFor);
                        string _ep = FindHTML(d, "\">", "<");
                        int ep = 0; if (!isMovie) ep = int.Parse(_ep);
                        if (ep == episode || isMovie) {
                            cloudGet = _cloudGet;
                            d = "";
                        }
                    }
                    /*{"html":"<div id=\"servers\">\n                        
    <div class=\"server row\" data-type=\"iframe\" data-id=\"28\">\n            
    <label class=\"name col-md-4 col-sm-5\">\n                
    <i class=\"fa fa-server\"><\/i>\n                 MyCloud\n            <\/label>\n            
    <div class=\"col-md-20 col-sm-19\">\n                                <ul class=\"episodes range active\"\n                    
    data-range-id=\"0\">\n                                        <li>\n                        
    <a  data-id=\"b5a4388f1a2fadb87f94fde2abf3a3e85288d96026d6be3bf4920f6aa718e12c\" href=\"\/film\/iron-man-3.885o\/1n4rzyv\">HD<\/a>\n                    
    <\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n                            
    <div class=\"server row\" data-type=\"iframe\" data-id=\"36\">\n            <label class=\"name col-md-4 col-sm-5\">\n                
    <i class=\"fa fa-server\"><\/i>\n                 F5 - HQ\n            <\/label>\n            <div class=\"col-md-20 col-sm-19\">\n                                
    <ul class=\"episodes range active\"\n                    data-range-id=\"0\">\n                                        <li>\n                        
    <a  data-id=\"05912e0bb9a837fc540e8ddc66beb8f2047667d192b2bee4aa9ba8e744f0eaea\" href=\"\/film\/iron-man-3.885o\/pr65wqx\">HD<\/a>\n                    
    <\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n                            
    <div class=\"server row\" data-type=\"iframe\" data-id=\"39\">\n            <label class=\"name col-md-4 col-sm-5\">\n                
    <i class=\"fa fa-server\"><\/i>\n                 Hydrax\n            <\/label>\n            <div class=\"col-md-20 col-sm-19\">\n                                
    <ul class=\"episodes range active\"\n                    data-range-id=\"0\">\n                                        <li>\n                        
    <a class=\"active\" data-id=\"41d881669367be4d23b70715f40410adac4788764836c1b80801639f08621e96\" href=\"\/film\/iron-man-3.885o\/m280668\">HD<\/a>\n                    
    <\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n            <\/div>"}


    https://prettyfast.to/e/66vvrk\/fe1541bb8d2aeaec6bb7e500d070b2ec?sub=https%253A%252F%252Fstaticf.akacdn.ru%252Ff%252Fsubtitle%252F7309.vtt%253Fv1*/
                    // https://fmovies.to/ajax/episode/info?ts=1574168400&_=694&id=d49ac231d1ddf83114eadf1234a1f5d8136dc4a5b6db299d037c06804b37b1ab&server=28
                    // https://fmovies.to/ajax/episode/info?ts=1574168400&_=199&id=1c7493cc7bf3cc16831ff9bf1599ceb6f4be2a65a57143c5a24c2dbea99104de&server=97
                    d = "";
                    int errorCount = 0;
                    while (d == "" && errorCount < 10) {
                        errorCount++;
                        string rD = "https://fmovies.to/ajax/episode/info?ts=" + dataTs + "&_=" + Random(100, 999) + "&id=" + cloudGet + "&server=" + Random(1, 99);
                        print(rD);
                        d = HTMLGet(rD, "https://fmovies.to");
                    }
                    if (d != "") {
                        string lookFor = "\"target\":\"";
                        while (d.Contains(lookFor)) {
                            string __url = FindHTML(d, lookFor, "\"").Replace("\\/", "/");
                            string dl = HTMLGet(__url, "https://fmovies.to");
                            string _lookFor = "\"file\":\"";
                            while (dl.Contains(_lookFor)) {
                                string __link = FindHTML(dl, _lookFor, "\"");
                                if (__link != "") {

                                    AddPotentialLink(normlaEpisode, __link, "HD FMovies", -1);  //"https://bharadwajpro.github.io/m3u8-player/player/#"+ __link, "HD FMovies", 30); // https://bharadwajpro.github.io/m3u8-player/player/#
                                }
                                dl = RemoveOne(dl, _lookFor);
                            }
                            d = RemoveOne(d, lookFor);
                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "GetFmoviesLinks";
            tempThred.Thread.Start();
        }

        static void GetLiveMovies123Links(int normalEpisode, int episode, int season, bool isMovie, string provider = "https://c123movies.com") // https://movies123.live & https://c123movies.com
        {
            TempThred tempThred = new TempThred();

            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string _title = ToDown(activeMovie.title.name, replaceSpace: "-");

                    string _url = (isMovie ? (provider + "/movies/" + _title) : (provider + "/episodes/" + _title + "-season-" + season + "-episode-" + episode));
                    print("___URL::" + _url);

                    string d = DownloadString(_url);
                    if (!GetThredActive(tempThred)) { return; };
                    string release = FindHTML(d, "Release:</strong> ", "<");
                    print("RELESE:::" + release + "::" + activeMovie.title.ogYear + "::");
                    bool succ = true;
                    if (release != activeMovie.title.ogYear) {
                        succ = false;
                        if (isMovie) {
                            d = DownloadString(_url + "-1");
                            succ = true;
                        }
                    }
                    if (succ) {
                        string live = FindHTML(d, "getlink(\'", "\'");
                        print("LIVE::" + live);
                        if (live != "") {
                            string url = provider + "/ajax/get-link.php?id=" + live + "&type=" + (isMovie ? "movie" : "tv") + "&link=sw&" + (isMovie ? "season=undefined&episode=undefined" : ("season=" + season + "&episode=" + episode));
                            print("MegaURL:" + url);
                            d = DownloadString(url); if (!GetThredActive(tempThred)) { return; };

                            string shortURL = FindHTML(d, "iframe src=\\\"", "\"").Replace("\\/", "/");
                            d = DownloadString(shortURL); if (!GetThredActive(tempThred)) { return; };

                            AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "GetLiveMovies123Links";
            tempThred.Thread.Start();
        }

        static void AddEpisodesFromMirrors(TempThred tempThred, string d, int normalEpisode) // DONT DO THEVIDEO provider, THEY USE GOOGLE CAPTCH TO VERIFY AUTOR; LOOK AT https://vev.io/api/serve/video/qy3pw89xwmr7 IT IS A POST REQUEST
        {
            string mp4 = "https://www.mp4upload.com/embed-" + FindHTML(d, "data-video=\"https://www.mp4upload.com/embed-", "\"");
            if (mp4 != "https://www.mp4upload.com/embed-") {
                try {
                    string _d = DownloadString(mp4, tempThred);
                    if (!GetThredActive(tempThred)) { return; };
                    string mxLink = Getmp4UploadByFile(_d);
                    AddPotentialLink(normalEpisode, mxLink, "Mp4Upload", 9);
                }
                catch (System.Exception) {
                }
            }
            string __d = d.ToString();
            string lookFor = "https://redirector.googlevideo.com/";
            int prio = 11;
            while (__d.Contains(lookFor)) {
                prio++;
                __d = "|:" + RemoveOne(__d, lookFor);
                string all = FindHTML(__d, "|", "}");
                string url = FindHTML(all, ":", "\'");
                string label = FindHTML(all, "label: \'", "\'").Replace(" P", "p");
                AddPotentialLink(normalEpisode, "h" + url, "GoogleVideo " + label, prio);
            }
            bool fembedAdded = LookForFembedInString(tempThred, normalEpisode, d);

            string nameId = "Vidstreaming";
            string vid = FindHTML(d, "data-video=\"//vidstreaming.io/streaming.php?", "\"");
            string beforeId = "https://vidstreaming.io/download?id=";
            if (vid == "") {
                vid = FindHTML(d, "//vidstreaming.io/streaming.php?", "\"");
            }
            if (vid == "") {
                vid = FindHTML(d, "//vidnode.net/load.php?id=", "\"");
                if (vid != "") {
                    beforeId = "https://vidnode.net/download?id=";
                    nameId = "VidNode";
                }
            }
            if (vid == "") {
                vid = FindHTML(d, "//vidnode.net/streaming.php?id=", "\"");
                if (vid != "") {
                    beforeId = "https://vidnode.net/download?id=";
                    nameId = "VidNode";
                }
            }

            if (vid == "") {
                vid = FindHTML(d, "//vidcloud9.com/download?id=", "\"");

                if (vid != "") {
                    beforeId = "https://vidcloud9.com/download?id=";
                    nameId = "VidCloud";
                }
            }
            print(">>STREAM::" + vid);
            if (vid != "") {
                string dLink = beforeId + vid.Replace("id=", "");
                string _d = DownloadString(dLink, tempThred);

                //https://gcloud.live/v/ky5g0h3zqylzmq4#caption=https://xcdnfile.com/sub/iron-man-hd-720p/iron-man-hd-720p.vtt

                if (!GetThredActive(tempThred)) { return; };

                GetVidNode(_d, normalEpisode, nameId);
                // if (!fembedAdded) {
                string fMds = dLink.Replace("download", "streaming.php");
                print("FMEMEDST: " + fMds);
                string ___d = DownloadString(fMds, tempThred);
                if (!GetThredActive(tempThred)) { return; };
                LookForFembedInString(tempThred, normalEpisode, ___d);
                // }



                /* // OLD CODE, ONLY 403 ERROR DOSEN'T WORK ANYMORE
                vid = "http://vidstreaming.io/streaming.php?" + vid;
                string _d = DownloadString(vid); if (!GetThredActive(tempThred)) { return; };
                string mxLink = FindHTML(_d, "sources:[{file: \'", "\'");
                print("Browser: " + vid + " | RAW (NO ADS): " + mxLink);
                if (CheckIfURLIsValid(mxLink)) {
                    Episode ep = activeMovie.episodes[normalEpisode];
                    if (ep.links == null) {
                        activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating };
                    }
                    activeMovie.episodes[normalEpisode].links.Add(new Link() { priority = 0, url = mxLink, name = "Vidstreaming" }); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                    linkAdded?.Invoke(null, 2);

                }
                */
            }
            else {
                print("Error :(");
            }
        }


        static void GetThe123movies(int normalEpisode, int episode, int season, bool isMovie)
        {
            TempThred tempThred = new TempThred();

            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string extra = ToDown(activeMovie.title.name, true, "-") + (isMovie ? ("-" + activeMovie.title.ogYear) : ("-" + season + "x" + episode));
                    string d = DownloadString("https://on.the123movies.eu/" + extra);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    string ts = FindHTML(d, "data-vs=\"", "\"");
                    print("DATATS::" + ts);
                    d = DownloadString(ts);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "GetThe123movies Thread";
            tempThred.Thread.Start();

        }


        public static void GetEpisodeLink(int episode = -1, int season = 1, bool purgeCurrentLinkThread = true, bool onlyEpsCount = false, bool isDub = true)
        {
            if (activeMovie.episodes == null) {
                return;
            }

            if (purgeCurrentLinkThread) {
                PurgeThreds(3);
            }

            TempThred tempThred = new TempThred();

            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+"); // THE URL SEARCH STRING

                    bool animeSeach = activeMovie.title.movieType == MovieType.Anime && ANIME_ENABLED; // || activeMovie.title.movieType == MovieType.AnimeMovie &&
                    bool movieSearch = activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie || activeMovie.title.movieType == MovieType.TVSeries;



                    // --------- CLEAR EPISODE ---------
                    int normalEpisode = episode == -1 ? 0 : episode - 1;                     //normalEp = ep-1;



                    activeMovie.subtitles = new List<Subtitle>(); // CLEAR SUBTITLES
                    DownloadSubtitlesAndAdd(isEpisode: (activeMovie.title.movieType == MovieType.TVSeries || activeMovie.title.movieType == MovieType.Anime), episodeCounter: normalEpisode); // CHANGE LANG TO USER SETTINGS


                    if (activeMovie.episodes.Count <= normalEpisode) { activeMovie.episodes.Add(new Episode()); }
                    Episode cEpisode = activeMovie.episodes[normalEpisode];
                    activeMovie.episodes[normalEpisode] = new Episode() {
                        links = new List<Link>(),
                        posterUrl = cEpisode.posterUrl,
                        rating = cEpisode.rating,
                        name = cEpisode.name,
                        date = cEpisode.date,
                        description = cEpisode.description,
                        id = cEpisode.id,
                    };

                    if (animeSeach) { // use https://www3.gogoanime.io/ or https://vidstreaming.io/

                        while (!activeMovie.title.MALData.done) {
                            Thread.Sleep(100);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        }

                        int _episode = int.Parse(episode.ToString()); // READ ONLY

                        if (isDub) {
                            TempThred tempthread = new TempThred();
                            tempthread.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                            tempthread.Thread = new System.Threading.Thread(() => {
                                try {
                                    print("DUBBED::" + episode + "|" + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum());
                                    if (episode <= activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum()) {
                                        List<string> fwords = GetAllDubbedAnimeLinks(activeMovie, season);
                                        print("SLUG1." + fwords[0]);
                                        int sel = -1;
                                        int floor = 0;
                                        int subtract = 0;
                                        if (activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason != null) {
                                            for (int i = 0; i < activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Count; i++) {
                                                int seling = floor + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];

                                                if (episode > floor && episode <= seling) {
                                                    sel = i;
                                                    subtract = floor;

                                                }
                                                //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                                                floor += activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];
                                            }
                                        }

                                        string fwordLink = fwords[sel];
                                        print("SLUGOS: " + fwordLink);
                                        DubbedAnimeEpisode dubbedEp = GetDubbedAnimeEpisode(fwordLink, _episode - subtract);

                                        string serverUrls = dubbedEp.serversHTML;
                                        string sLookFor = "hl=\\\"";
                                        while (serverUrls.Contains(sLookFor)) {
                                            string baseUrl = FindHTML(dubbedEp.serversHTML, "hl=\\\"", "\"");
                                            print("BASE::" + baseUrl);
                                            string burl = "https://bestdubbedanime.com/xz/api/playeri.php?url=" + baseUrl + "&_=" + UnixTime;
                                            print(burl);
                                            string _d = DownloadString(burl);
                                            print("SSC:" + _d);
                                            int prio = 20;

                                            string enlink = "\'";
                                            if (_d.Contains("<source src=\"")) {
                                                enlink = "\"";
                                            }
                                            string lookFor = "<source src=" + enlink;
                                            while (_d.Contains(lookFor)) {
                                                string vUrl = FindHTML(_d, lookFor, enlink);
                                                if (vUrl != "") {
                                                    vUrl = "https:" + vUrl;
                                                }
                                                string label = FindHTML(_d, "label=" + enlink, enlink);
                                                print(vUrl + "|" + label);
                                                AddPotentialLink(normalEpisode, vUrl, "DubbedAnime " + label.Replace("0p", "0") + "p", prio);

                                                _d = RemoveOne(_d, lookFor);
                                                _d = RemoveOne(_d, "label=" + enlink);
                                            }
                                            serverUrls = RemoveOne(serverUrls, sLookFor);
                                        }
                                    }
                                }
                                finally {
                                    JoinThred(tempthread);
                                }
                            });
                            tempthread.Thread.Name = "DubAnime Thread";
                            tempthread.Thread.Start();

                        }


                        try {
                            if (episode <= activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Sum()) {
                                string fwordLink = "";
                                List<string> fwords = GetAllGogoLinksFromAnime(activeMovie, season, isDub);
                                // for (int i = 0; i < fwords.Count; i++) {
                                // print("FW: " + fwords[i]);
                                //  }

                                // --------------- GET WHAT SEASON THE EPISODE IS IN ---------------

                                int sel = -1;
                                int floor = 0;
                                int subtract = 0;
                                // print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason);
                                if (activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason != null) {
                                    for (int i = 0; i < activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Count; i++) {
                                        int seling = floor + activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];

                                        if (episode > floor && episode <= seling) {
                                            sel = i;
                                            subtract = floor;

                                        }
                                        //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                                        floor += activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];
                                    }
                                }
                                //print("sel: " + sel);
                                if (sel != -1) {
                                    try {
                                        fwordLink = fwords[sel].Replace("-dub", "") + (isDub ? "-dub" : "");
                                    }
                                    catch (Exception) {

                                    }
                                }

                                if (fwordLink != "") { // IF FOUND
                                    string dstring = "https://www3.gogoanime.io/" + fwordLink + "-episode-" + (_episode - subtract);
                                    print("DSTRING: " + dstring);
                                    string d = DownloadString(dstring, tempThred);

                                    AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                                }
                            }
                        }
                        catch (Exception) {
                            print("GOGOANIME ERROR");
                        }


                    }
                    if (movieSearch) { // use https://movies123.pro/

                        // --------- SETTINGS ---------

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        // -------------------- HD MIRRORS --------------------

                        bool isMovie = activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie;
                        if (isMovie) {
                            AddFastMovieLink(normalEpisode);
                            AddFastMovieLink2(normalEpisode);
                        }
                        else if (activeMovie.title.movieType == MovieType.TVSeries) {
                            GetTMDB(episode, season, normalEpisode);
                            GetWatchTV(season, episode, normalEpisode);
                        }
                        GetFmoviesLinks(normalEpisode, episode, season);
                        GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://movies123.live");
                        GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://c123movies.com");
                        GetThe123movies(normalEpisode, episode, season, isMovie);

                        if (activeMovie.title.yesmoviessSeasonDatas != null) {
                            for (int i = 0; i < activeMovie.title.yesmoviessSeasonDatas.Count; i++) {
                                //     print(activeMovie.title.yesmoviessSeasonDatas[i].id + "<-IDS:" + season);
                                if (activeMovie.title.yesmoviessSeasonDatas[i].id == (isMovie ? 1 : season)) {
                                    YesMovies(normalEpisode, activeMovie.title.yesmoviessSeasonDatas[i].url);
                                }
                            }
                        }
                        GetLinksFromWatchSeries(season, normalEpisode);
                        if (GOMOSTEAM_ENABLED) {
                            TempThred minorTempThred = new TempThred();
                            minorTempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                            minorTempThred.Thread = new System.Threading.Thread(() => {
                                try {
                                    string find = activeMovie.title.name.ToLower() + (activeMovie.title.movieType == MovieType.TVSeries ? "-season-" + season : "");
                                    find = find.Replace("\'", "-");
                                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                                    find = rgx.Replace(find, "");


                                    find = find.Replace(" - ", "-").Replace(" ", "-");

                                    if (activeMovie.title.movieType == MovieType.TVSeries) { // ADD CORRECT FORMAT; https://gomostream.com/show/game-of-thrones/01-01
                                        find = find.Replace("-season-", "/");

                                        for (int i = 0; i < 10; i++) {
                                            if (find.EndsWith("/" + i)) {
                                                find = find.Replace("/" + i, "/0" + i);
                                            }
                                        }

                                        if (episode.ToString() != "-1") {
                                            find += "-" + episode;
                                        }

                                        for (int i = 0; i < 10; i++) {
                                            if (find.EndsWith("-" + i)) {
                                                find = find.Replace("-" + i, "-0" + i);
                                            }
                                        }
                                    }

                                    string gomoUrl = "https://" + GOMOURL + "/" + ((activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie) ? "movie" : "show") + "/" + find;
                                    print(gomoUrl);
                                    DownloadGomoSteam(gomoUrl, tempThred, normalEpisode);
                                }
                                finally {
                                    JoinThred(minorTempThred);
                                }
                            });
                            minorTempThred.Thread.Name = "Mirror Thread";
                            minorTempThred.Thread.Start();
                        }
                        if (SUBHDMIRROS_ENABLED) {
                            if (activeMovie.title.movies123MetaData.movieLink != null) {
                                if (activeMovie.title.movieType == MovieType.TVSeries) {
                                    int normalSeason = season - 1;
                                    List<Movies123SeasonData> seasonData = activeMovie.title.movies123MetaData.seasonData;
                                    // ---- TO PREVENT ERRORS START ----
                                    if (seasonData != null) {
                                        if (seasonData.Count > normalSeason) {
                                            if (seasonData[normalSeason].episodeUrls != null) {
                                                if (seasonData[normalSeason].episodeUrls.Count > normalEpisode) {
                                                    // ---- END ----
                                                    string fwordLink = seasonData[normalSeason].seasonUrl + "/" + seasonData[normalSeason].episodeUrls[normalEpisode];
                                                    print(fwordLink);
                                                    for (int f = 0; f < MIRROR_COUNT; f++) {
                                                        GetLinkServer(f, fwordLink, tempThred, normalEpisode);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else {
                                    for (int f = 0; f < MIRROR_COUNT; f++) {
                                        print(">::" + f);
                                        GetLinkServer(f, activeMovie.title.movies123MetaData.movieLink, tempThred); // JUST GET THE MOVIE
                                    }
                                }
                            }

                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Get Links";
            tempThred.Thread.Start();
        }

        static void GetVidNode(string _d, int normalEpisode, string urlName = "Vidstreaming")
        {
            string linkContext = FindHTML(_d, "<h6>Link download</h6>", " </div>");
            string lookFor = "href=\"";
            string rem = "<div class=<\"dowload\"><a";
            linkContext = RemoveOne(linkContext, rem);
            int prio = 0;
            while (linkContext.Contains(lookFor)) {
                string link = FindHTML(linkContext, lookFor, "\"");
                string _nameContext = FindHTML(linkContext, link, "</a></div>") + "</a></div>";
                string name = urlName + " (" + FindHTML(_nameContext, "            (", "</a></div>");
                link = link.Replace("&amp;", "&");

                print("LINK: " + link + "|" + name);
                name = name.Replace("(", "").Replace(")", "").Replace("mp4", "").Replace("orginalP", "Source").Replace("-", "").Replace("0P", "0p");

                if (CheckIfURLIsValid(link)) {
                    prio++;
                    /*
                    Episode ep = activeMovie.episodes[normalEpisode];
                    if (ep.links == null) {
                        activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                    }
                    activeMovie.episodes[normalEpisode].links.Add(new Link() { priority = prio, url = link, name = name }); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                    linkAdded?.Invoke(null, 1);*/
                    AddPotentialLink(normalEpisode, link, name, prio);
                }
                linkContext = RemoveOne(linkContext, lookFor);
            }
        }

        public static void GetFembed(string fembed, TempThred tempThred, int normalEpisode, string urlType = "https://www.fembed.com", string referer = "www.fembed.com")
        {
            if (fembed != "") {
                int prio = 10;
                string _d = PostRequest(urlType + "/api/source/" + fembed, urlType + "/v/" + fembed, "r=&d=" + referer, tempThred);
                if (_d != "") {
                    string lookFor = "\"file\":\"";
                    string _labelFind = "\"label\":\"";
                    while (_d.Contains(_labelFind)) {
                        string link = FindHTML(_d, lookFor, "\",\"");

                        //  d = RemoveOne(d, link);
                        link = link.Replace("\\/", "/");

                        string label = FindHTML(_d, _labelFind, "\"");
                        print(label + "|" + link);
                        if (CheckIfURLIsValid(link)) {
                            prio++;
                            AddPotentialLink(normalEpisode, link, "XStream " + label, prio);
                        }
                        _d = RemoveOne(_d, _labelFind);
                    }
                }
            }
        }

        //https://www.freefullmovies.zone/movies/watch.Iron-Man-3-2013.movie.html
        //(LOW QUILITY) see https://1watchfree.me/free-avengers-infinity-war-online-movie-001/ ; get  //upfiles.pro/embed-mde1uxevydps.html ip =FIND[[[ <img src="http:// ,,,, / ]]] id = FIND [[[ mp4| ,,, |sources ]]] for more providers ||| full url = https:// ip / id /v.mp4
        static void AddFastMovieLink(int episode)
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string d = DownloadString("https://www.freefullmovies.zone/movies/watch." + ToDown(activeMovie.title.name, true, "-").Replace(" ", "-") + "-" + activeMovie.title.year.Substring(0, 4) + ".movie.html", tempThred);

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    string find = "<source src=\"";
                    string link = FindHTML(d, find, "\"");
                    if (link != "") {
                        double dSize = GetFileSize(link);
                        if (dSize > 100) { // TO REMOVE TRAILERS
                            AddPotentialLink(episode, link, "HD FullMovies", 13);
                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "FullMovies";
            tempThred.Thread.Start();
        }

        static void GetMovieTv(int episode, string d, TempThred tempThred) // https://1movietv.com/1movietv-streaming-api/ 
        {
            if (d != "") {
                string find = FindHTML(d, "src=\"https://myvidis.top/v/", "\"");
                int prio = 0;
                if (find != "") {
                    string _d = PostRequest("https://myvidis.top/api/source/" + find, "https://myvidis.top/v/" + find, "", tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    if (_d != "") {
                        string lookFor = "\"file\":\"";
                        string _labelFind = "\"label\":\"";
                        while (_d.Contains(_labelFind)) {
                            string link = FindHTML(_d, lookFor, "\",\"");
                            //  d = RemoveOne(d, link);
                            link = link.Replace("\\/", "/");

                            string label = FindHTML(_d, _labelFind, "\"");
                            print(label + "|" + link);
                            if (CheckIfURLIsValid(link)) {
                                prio++;
                                AddPotentialLink(episode, link, "MovieTv " + label, prio);
                            }
                            _d = RemoveOne(_d, _labelFind);
                        }
                    }
                }
            }
        }

        static void AddFastMovieLink2(int episode) // https://1movietv.com/1movietv-streaming-api/
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string d = DownloadString("https://1movietv.com/playstream/" + activeMovie.title.id, tempThred);
                    GetMovieTv(episode, d, tempThred);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Movietv";
            tempThred.Thread.Start();
        }

        static void GetTMDB(int episode, int season, int normalEpisode)// https://1movietv.com/1movietv-streaming-api/
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string d = DownloadString("https://www.themoviedb.org/search/tv?query=" + activeMovie.title.name + "&language=en-US");
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    if (d != "") {
                        string tmdbId = FindHTML(d, "<a id=\"tv_", "\"");
                        if (tmdbId != "") {
                            string _d = DownloadString("https://1movietv.com/playstream/" + tmdbId + "-" + season + "-" + episode, tempThred);
                            GetMovieTv(normalEpisode, _d, tempThred);
                            //https://1movietv.com/playstream/71340-2-8
                        }
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Movietv";
            tempThred.Thread.Start();
        }

        public static double GetFileSize(string url)
        {
            try {
                var webRequest = HttpWebRequest.Create(new System.Uri(url));
                webRequest.Method = "HEAD";

                using (var webResponse = webRequest.GetResponse()) {
                    var fileSize = webResponse.Headers.Get("Content-Length");
                    var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / Math.Pow((double)App.GetSizeOfJumpOnSystem(), 2.0), 2);
                    return fileSizeInMegaByte;
                }
            }
            catch (Exception) {
                return -1;
            }
        }

        public static double GetFileSizeOnSystem(string path)
        {
            try {
                return Math.Round(Convert.ToDouble(GetFileBytesOnSystem(path)) / Math.Pow((double)App.GetSizeOfJumpOnSystem(), 2.0), 2);
            }
            catch (Exception) {
                return -1;
            }
        }

        public static long GetFileBytesOnSystem(string path)
        {
            try {
                return new System.IO.FileInfo(path).Length;
            }
            catch (Exception) {
                return -1;
            }
        }

        public static bool GetSettings(MovieType type = MovieType.Movie)
        {
            return true;
        }

        public static void AddToActiveSearchResults(Poster p)
        {
            if (!activeSearchResults.Contains(p)) {

                bool add = true;
                for (int i = 0; i < activeSearchResults.Count; i++) {
                    if (activeSearchResults[i].posterUrl == p.posterUrl) {
                        add = false;
                    }
                }
                if (add) {
                    //print(p.name + "|" + p.posterUrl);
                    activeSearchResults.Add(p);
                    addedSeachResult?.Invoke(null, p);
                }
            }
        }

        /* ------------------------------------------------ EXAMPLE THRED ------------------------------------------------

            TempThred tempThred = new TempThred();
            tempThred.typeId = 1; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() =>
            {
                try {


                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "QuickSearch";
            tempThred.Thread.Start();


            */

        public static string ConvertIMDbImagesToHD(string nonHDImg, int? pwidth = null, int? pheight = null)
        {
            string img = FindHTML("|" + nonHDImg, "|", "._");
            pheight = (int)Math.Round((pheight ?? 0) * posterRezMulti);
            pwidth = (int)Math.Round((pwidth ?? 0) * posterRezMulti);
            pheight = App.ConvertDPtoPx((int)pheight);
            pwidth = App.ConvertDPtoPx((int)pwidth);
            if (pwidth == 0 && pheight == 0) return nonHDImg;
            print("IMDBASE:" + nonHDImg);
            img += "." + (pheight > 0 ? "_UY" + pheight : "") + (pwidth > 0 ? "UX" + pwidth : "") + "_.jpg";
            print(img);
            /*
            string x1 = pwidth.ToString();
            string y1 = pheight.ToString();
            pheight = (int)Math.Round(pheight * mMulti * posterRezMulti);
            pwidth = (int)Math.Round(pwidth * mMulti * posterRezMulti);
            int zpwidth = (int)Math.Round(pwidth *zoom);
            int zpheight = (int)Math.Round(pheight *zoom);
            print("IMG::" + nonHDImg);
            string img = nonHDImg.Replace("," + x1 + "," + y1 + "_AL", "," + pwidth + "," + pheight + "_AL").Replace("," + y1 + "," + x1 + "_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY" + y1, "UY" + zpheight).Replace("UX" + x1, "UX" + zpwidth).Replace("UX" + y1, "UX" + zpheight).Replace("UY" + x1, "UY" + zpwidth);
            print("IMG>>" + img);*/
            return img;
        }

        static void YesMovies(int normalEpisode, string url) // MIRROR https://cmovies.tv/ 
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    int episode = normalEpisode + 1;
                    string d = DownloadString(url.Replace("watching.html", "") + "watching.html");

                    string movieId = FindHTML(d, "var movie_id = \'", "\'");
                    if (movieId == "") return;

                    d = DownloadString("https://yesmoviess.to/ajax/v2_get_episodes/" + movieId);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string episodeId = FindHTML(d, "title=\"Episode " + episode + "\" class=\"btn-eps\" episode-id=\"", "\"");
                    if (episodeId == "") return;
                    d = DownloadString("https://yesmoviess.to/ajax/load_embed/mov" + episodeId);

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string embedededUrl = FindHTML(d, "\"embed_url\":\"", "\"").Replace("\\", "") + "=EndAll";
                    string __url = FindHTML(embedededUrl, "id=", "=EndAll");
                    if (__url == "") return;
                    embedededUrl = "https://video.opencdn.co/api/?id=" + __url;
                    print(embedededUrl + "<<<<<<<<<<<<<<<<");
                    d = DownloadString(embedededUrl);

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    string link = FindHTML(d, "\"link\":\"", "\"").Replace("\\", "").Replace("//", "https://").Replace("https:https:", "https:");
                    print("LINK:" + link);
                    d = DownloadString(link);

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string secondLink = FindHTML(d, "https://vidnode.net/download?id=", "\"");
                    print("FIRST: " + secondLink);
                    if (secondLink != "") {
                        d = DownloadString("https://vidnode.net/download?id=" + secondLink);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        GetVidNode(d, normalEpisode);
                    }
                    LookForFembedInString(tempThred, normalEpisode, d);
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "YesMovies";
            tempThred.Thread.Start();
        }

        // -------------------- METHODS --------------------
        static string HTMLGet(string uri, string referer, bool br = false)
        {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                request.Method = "GET";
                request.ContentType = "text/html; charset=UTF-8";
                // webRequest.Headers.Add("Host", "trollvid.net");
                request.UserAgent = USERAGENT;
                request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Referer = referer;

                // webRequest.Headers.Add("Cookie", "__cfduid=dc6e854c3f07d2a427bca847e1ad5fa741562456483; _ga=GA1.2.742704858.1562456488; _gid=GA1.2.1493684150.1562456488; _maven_=popped; _pop_=popped");
                request.Headers.Add("TE", "Trailers");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    //  print(response.GetResponseHeader("set-cookie").ToString());


                    // using (Stream stream = response.GetResponseStream())
                    if (br) {
                        /*
                        using (BrotliStream bs = new BrotliStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress)) {
                            using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream()) {
                                bs.CopyTo(msOutput);
                                msOutput.Seek(0, System.IO.SeekOrigin.Begin);
                                using (StreamReader reader = new StreamReader(msOutput)) {
                                    string result = reader.ReadToEnd();

                                    return result;

                                }
                            }
                        }
                        */
                        return "";
                    }
                    else {
                        using (Stream stream = response.GetResponseStream()) {
                            // print("res" + response.StatusCode);
                            foreach (string e in response.Headers) {
                                // print("Head: " + e);
                            }
                            // print("LINK:" + response.GetResponseHeader("Set-Cookie"));
                            using (StreamReader reader = new StreamReader(stream)) {
                                string result = reader.ReadToEnd();
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// WHEN DOWNLOADSTRING DOSNE'T WORK, BASILCY SAME THING, BUT CAN ALSO BE USED TO FORCE ENGLISH
        /// </summary>
        /// <param name="url"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public static string GetHTML(string url, bool en = true)
        {
            string html = string.Empty;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                // List<string> heads = new List<string>(); // HEADERS
                /*
                heads = HeadAdd("");
                for (int i = 0; i < heads.Count; i++) {
                    try {
                        request.Headers.Add(HeadToRes(heads[i], 0), HeadToRes(heads[i], 1));
                        print("PRO:" + HeadToRes(heads[i], 0) + ": " + HeadToRes(heads[i], 1));

                    }
                    catch (Exception) {

                    }
                }
                */
                WebHeaderCollection myWebHeaderCollection = request.Headers;
                if (en) {
                    myWebHeaderCollection.Add("Accept-Language", "en;q=0.8");
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.UserAgent = USERAGENT;
                request.Referer = url;
                //request.AddRange(1212416);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())

                using (StreamReader reader = new StreamReader(stream)) {
                    html = reader.ReadToEnd();
                }
                return html;
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// WHEN DOWNLOADSTRING DOSNE'T WORK, BASILCY SAME THING, BUT CAN ALSO BE USED TO FORCE ENGLISH
        /// </summary>
        /// <param name="url"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public static async Task<string> GetHTMLAsync(string url, bool en = true)
        {
            string html = string.Empty;
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                WebHeaderCollection myWebHeaderCollection = request.Headers;
                if (en) {
                    myWebHeaderCollection.Add("Accept-Language", "en;q=0.8");
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.UserAgent = USERAGENT;
                request.Referer = url;
                //request.AddRange(1212416);

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())

                using (StreamReader reader = new StreamReader(stream)) {
                    html = reader.ReadToEnd();
                }
                return html;
            }
            catch (Exception) {
                return "";
            }
        }

        static string ReadJson(string all, string inp)
        {
            try {
                int indexInp = all.IndexOf(inp);
                if (indexInp == -1) {
                    return "";
                }
                string newS = all.Substring(indexInp + (inp.Length + 3), all.Length - indexInp - (inp.Length + 3));

                string ns = newS.Substring(0, newS.IndexOf("\""));

                return ns;
            }
            catch (Exception) {
                return "";
            }
        }

        public static bool AddPotentialLink(int normalEpisode, string _url, string _name, int _priority)
        {
            if (_url == "http://error.com") return false; // ERROR
            if (_url.Replace(" ", "") == "") return false;

            _name = _name.Replace("  ", " ");
            _url = _url.Replace(" ", "%20");
            if (!LinkListContainsString(activeMovie.episodes[normalEpisode].links, _url)) {
                if (CheckIfURLIsValid(_url)) {
                    print("ADD LINK:" + normalEpisode + "|" + _name + "|" + _priority + "|" + _url);
                    Episode ep = activeMovie.episodes[normalEpisode];
                    if (ep.links == null) {
                        activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                    }

                    bool done = false;
                    int count = 1;
                    string realName = _name;
                    while (!done && !realName.Contains("[MIRRORCOUNTER]")) {
                        count++;
                        done = true;
                        for (int i = 0; i < ep.links.Count; i++) {
                            if (ep.links[i].name == realName) {
                                realName = _name + " (Mirror " + count + ")";
                                done = false;
                                break;
                            }
                        }
                    }
                    realName = realName.Replace("  ", " ");
                    var link = new Link() { priority = _priority, url = _url, name = realName };
                    activeMovie.episodes[normalEpisode].links.Add(link); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                    linkAdded?.Invoke(null, link);
                    return true;
                }
            }
            return false;
        }

        public static DubbedAnimeEpisode GetDubbedAnimeEpisode(string slug, int eps)
        {
            string url = "https://bestdubbedanime.com/xz/v3/jsonEpi.php?slug=" + slug + "/" + eps + "&_=" + UnixTime;
            string d = DownloadString(url);
            print(d);

            const string smallAdd = "\": \"";
            string FastFind(string name)
            {
                if (d.Contains(name + smallAdd + "\": null,")) {
                    return "";
                }
                string _f = FindHTML(d, name + smallAdd, "\",");
                if (_f == "") {
                    _f = FindHTML(d, name + "\":", ",");
                }
                return _f;
            }
            int FastId(string name)
            {
                try {
                    return int.Parse(FastFind(name));
                }
                catch (Exception) {
                    return -1;
                }
            }

            DubbedAnimeEpisode e = new DubbedAnimeEpisode();
            try {
                e.serversHTML = FastFind(nameof(e.serversHTML));

                e.ep = FastId(nameof(e.ep));
                e.Epviews = FastId(nameof(e.Epviews));
                e.rowid = FastId(nameof(e.ep));
                e.totalEp = FastId(nameof(e.totalEp));
                e.year = FastId(nameof(e.year));
                e.showid = FastId(nameof(e.showid));
                e.TotalViews = FastId(nameof(e.TotalViews));

                e.desc = FastFind(nameof(e.desc));
                e.title = FastFind(nameof(e.title));
                e.slug = FastFind(nameof(e.slug));
                e.status = FastFind(nameof(e.status));
            }
            catch (Exception) {
                print("!!!!!!DubbedAnimeEpisodeERROR");
            }
            return e;
        }

        public static int UnixTime { get { return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; } }

        /// <summary>
        /// GET LOWHD MIRROR SERVER USED BY MOVIES123 AND PLACE THEM IN ACTIVEMOVIE
        /// </summary>
        /// <param name="f"></param>
        /// <param name="realMoveLink"></param>
        /// <param name="tempThred"></param>
        /// <param name="episode"></param>
        public static void GetLinkServer(int f, string realMoveLink, TempThred tempThred, int episode = 0)
        {
            TempThred minorTempThred = new TempThred();
            minorTempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            minorTempThred.Thread = new System.Threading.Thread(() => {
                try {
                    //  int unixTimestamp = ;

                    string jsn = GetWebRequest(realMoveLink + "?server=server_" + f + "&_=" + UnixTime);

                    if (!GetThredActive(minorTempThred)) { return; };  // ---- THREAD CANCELLED ----

                    while (jsn.Contains("http")) {
                        int _start = jsn.IndexOf("http");
                        jsn = jsn.Substring(_start, jsn.Length - _start);
                        int id = jsn.IndexOf("\"");
                        if (id != -1) {
                            string newM = jsn.Substring(0, id);
                            newM = newM.Replace("\\", "");
                            print("::>" + newM);
                            AddPotentialLink(episode, newM, "Mirror [MIRRORCOUNTER]", 0);
                        }
                        jsn = jsn.Substring(4, jsn.Length - 4);
                    }
                }
                finally {
                    JoinThred(minorTempThred);
                }
            });
            minorTempThred.Thread.Name = "Mirror Thread";
            minorTempThred.Thread.Start();
        }

        /// <summary>
        /// GET IF URL IS VALID, null and "" will return false
        /// </summary>
        /// <param name="uriName"></param>
        /// <returns></returns>
        public static bool CheckIfURLIsValid(string uriName)
        {
            if (uriName == null) return false;
            if (uriName == "") return false;

            Uri uriResult;
            return Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// RETURNS THE TRUE MX URL OF A MP4 UPLOAD
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        static string Getmp4UploadByFile(string result)
        {
            result = result.Replace("||||", "|");
            result = result.Replace("|||", "|");
            result = result.Replace("||", "|");
            result = result.Replace("||", "|");
            result = result.Replace("||", "|");

            string server = "s1";
            for (int i = 0; i < 100; i++) {
                if (result.Contains("|s" + i + "|")) {
                    server = "s" + i;
                }
            }

            for (int i = 0; i < 100; i++) {
                if (result.Contains("|www" + i + "|")) {
                    server = "www" + i;
                }
            }

            int pos = result.IndexOf("vid|mp4|download");
            int offset = 18;

            if (pos == -1) {
                offset = 9;
                pos = result.IndexOf("vid|mp4");
            }
            if (pos == -1) {
                pos = result.IndexOf("mp4|video");
                offset = 11;
            }

            if (pos == -1) {
                return "";
                /*
                if (_episode.Contains("This video is no longer available due to a copyright claim")) {
                    break;
                }
                */
            }

            string r = "-1";
            string allEp = result.Substring(pos + offset - 1, result.Length - pos - offset + 1);
            if ((allEp.Substring(0, 30).Contains("|"))) {
                string rez = allEp.Substring(0, allEp.IndexOf("p")) + "p";
                r = rez;
                allEp = allEp.Substring(allEp.IndexOf("p") + 2, allEp.Length - allEp.IndexOf("p") - 2);
            }
            string urlLink = allEp.Substring(0, allEp.IndexOf("|"));

            allEp = allEp.Substring(urlLink.Length + 1, allEp.Length - urlLink.Length - 1);
            string typeID = allEp.Substring(0, allEp.IndexOf("|"));

            string _urlLink = FindReverseHTML(result, "|" + typeID + "|", "|");

            string mxLink = "https://" + server + ".mp4upload.com:" + typeID + "/d/" + _urlLink + "/video.mp4"; //  282 /d/qoxtvtduz3b4quuorgvegykwirnmt3wm3mrzjwqhae3zsw3fl7ajhcdj/video.mp4

            string addRez = "";
            if (r != "-1") {
                addRez += " | " + r;
            }

            if (typeID != "282") {
                //Error
            }
            else {

            }
            return mxLink;
        }

        static string ReadDataMovie(string all, string inp)
        {
            try {
                string newS = all.Substring(all.IndexOf(inp) + (inp.Length + 2), all.Length - all.IndexOf(inp) - (inp.Length + 2));
                string ns = newS.Substring(0, newS.IndexOf("\""));
                return ns;
            }
            catch (Exception) {
                return "";
            }
        }

        public static string FindReverseHTML(string all, string first, string end, int offset = 0)
        {
            int x = all.IndexOf(first);
            all = all.Substring(0, x);
            int y = all.LastIndexOf(end) + end.Length;
            //  print(x + "|" + y);
            return all.Substring(y, all.Length - y);
        }

        /// <summary>
        /// REMOVES ALL SPECIAL CHARACTERS
        /// </summary>
        /// <param name="text"></param>
        /// <param name="toLower"></param>
        /// <param name="replaceSpace"></param>
        /// <returns></returns>
        public static string ToDown(string text, bool toLower = true, string replaceSpace = " ")
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            try {
                text = rgx.Replace(text, "");

            }
            catch (Exception) {
                return text;
            }
            if (toLower) {
                text = text.ToLower();
            }
            text = text.Replace(" ", replaceSpace);
            return text;
        }

        static string ForceLetters(int inp, int letters = 2)
        {
            int added = letters - inp.ToString().Length;
            if (added > 0) {
                return MultiplyString("0", added) + inp.ToString();
            }
            else {
                return inp.ToString();
            }
        }

        public static string MultiplyString(string s, int times)
        {
            return String.Concat(Enumerable.Repeat(s, times));
        }

        /// <summary>
        /// NETFLIX like time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertTimeToString(double time)
        {
            int sec = (int)Math.Round(time);
            int rsec = (sec % 60);
            int min = (int)Math.Ceiling((sec - rsec) / 60f);
            int rmin = min % 60;
            int h = (int)Math.Ceiling((min - rmin) / 60f);
            int rh = h;// h % 24;
            return (h > 0 ? ForceLetters(h) + ":" : "") + ((rmin >= 0 || h >= 0) ? ForceLetters(rmin) + ":" : "") + ForceLetters(rsec);
        }
        private static string GetWebRequest(string url)
        {
            string WEBSERVICE_URL = url;
            try {
                var __webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (__webRequest != null) {
                    __webRequest.Method = "GET";
                    __webRequest.Timeout = 12000;
                    __webRequest.ContentType = "application/json";
                    __webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");

                    using (System.IO.Stream s = __webRequest.GetResponse().GetResponseStream()) {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s)) {
                            var jsonResponse = sr.ReadToEnd();
                            return jsonResponse.ToString();
                            // Console.WriteLine(String.Format("Response: {0}", jsonResponse));
                        }
                    }
                }
            }
            catch (System.Exception) { }
            return "";
        }

        /// <summary>
        /// GET GOMOSTEAM SITE MIRRORS
        /// </summary>
        /// <param name="url"></param>
        /// <param name="_tempThred"></param>
        /// <param name="episode"></param>
        static void DownloadGomoSteam(string url, TempThred _tempThred, int episode)
        {
            print("Downloading gomo: " + url);
            bool done = true;
            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    try {
                        string d = "";
                        if (d == "") {
                            try {
                                d = DownloadString(url, tempThred, false, 2); if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            }
                            catch (System.Exception) {
                                debug("Error gogo");
                            }
                        }

                        if (d == "") {
                            print("GetHTML SAVE");
                            d = GetHTML(url);
                            if (!GetThredActive(tempThred)) { return; };
                        }

                        if (d == "") {
                            d = HTMLGet(url, "https://" + GOMOURL);
                            if (d != "") {
                                print("HTMLGET SAVE");
                            }
                            if (!GetThredActive(tempThred)) { return; };
                        }

                        if (d != "") { // If not failed to connect
                            debug("Passed gogo download site");

                            // ----- JS EMULATION, CHECK USED BY WEBSITE TO STOP WEB SCRAPE BOTS, DID NOT STOP ME >:) -----

                            string tokenCode = FindHTML(d, "var tc = \'", "'");
                            string _token = FindHTML(d, "_token\": \"", "\"");
                            string funct = "function _tsd_tsd_ds(" + FindHTML(d, "function _tsd_tsd_ds(", "</script>").Replace("\"", "'") + " log(_tsd_tsd_ds('" + tokenCode + "'))";
                            // print(funct);
                            if (funct == "function _tsd_tsd_ds( log(_tsd_tsd_ds(''))") {
                                debug(d); // ERROR IN LOADING JS
                            }
                            string realXToken = "";
                            var engine = new Engine()
                            .SetValue("log", new Action<string>((a) => { realXToken = a; }));

                            engine.Execute(@funct);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                                                         //GetAPI(realXToken, tokenCode, _token, tempThred, episode);
                            print("PAssed js test" + realXToken);
                            System.Uri myUri = new System.Uri("https://" + GOMOURL + "/decoding_v3.php"); // Can't DownloadString because of RequestHeaders (Anti-bot)
                            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

                            // --- Headers ---

                            webRequest.Method = "POST";
                            webRequest.Headers.Add("x-token", realXToken);
                            webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                            webRequest.Headers.Add("DNT", "1");
                            webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
                            webRequest.Headers.Add("TE", "Trailers");
                            webRequest.Headers.Add("Pragma", "Trailers");
                            webRequest.ContentType = "application/x-www-form-urlencoded";
                            done = false;
                            print("Passed token");

                            webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
                                HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
                                Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

                                string requestBody = true ? ("tokenCode=" + tokenCode + "&_token=" + _token) : "type=epis&xny=hnk&id=" + tokenCode; // --- RequestHeaders ---

                                byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

                                postStream.Write(byteArray, 0, byteArray.Length);
                                postStream.Close();
                                print("PASSED TOKENPOST");

                                if (!GetThredActive(tempThred)) { return; };


                                // BEGIN RESPONSE

                                _webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
                                    HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
                                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
                                    using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                                        if (!GetThredActive(tempThred)) { print(":("); return; };
                                        print("GOT RESPONSE:");
                                        string result = httpWebStreamReader.ReadToEnd();
                                        print("RESULT:::" + result);
                                        try {
                                            if (result != "") {

                                                // --------------- GOT RESULT!!!!! ---------------

                                                WebClient client = new WebClient();

                                                // --------------- MIRROR LINKS ---------------
                                                string veryURL = FindHTML(result, "https:\\/\\/verystream.com\\/e\\/", "\"");
                                                string gunURL = "https://gounlimited.to/" + FindHTML(result, "https:\\/\\/gounlimited.to\\/", ".html") + ".html";
                                                string onlyURL = "https://onlystream.tv" + FindHTML(result, "https:\\/\\/onlystream.tv", "\"").Replace("\\", "");
                                                //string gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
                                                string upstream = FindHTML(result, "https:\\/\\/upstream.to\\/embed-", "\"");
                                                string mightyupload = FindHTML(result, "http:\\/\\/mightyupload.com\\/", "\"").Replace("\\/", "/");
                                                //["https:\/\/upstream.to\/embed-05mzggpp3ohg.html","https:\/\/gomo.to\/vid\/eyJ0eXBlIjoidHYiLCJzIjoiMDEiLCJlIjoiMDEiLCJpbWQiOiJ0dDA5NDQ5NDciLCJfIjoiMzQyMDk0MzQzMzE4NTEzNzY0IiwidG9rZW4iOiI2NjQ0MzkifQ,,&noneemb","https:\/\/hqq.tv\/player\/embed_player.php?vid=SGVsWVI5aUNlVTZxTTdTV09RY0x6UT09&autoplay=no",""]

                                                if (upstream != "") {
                                                    string _d = DownloadString("https://upstream.to/embed-" + upstream);
                                                    if (!GetThredActive(tempThred)) { return; };
                                                    string lookFor = "file:\"";
                                                    int prio = 16;
                                                    while (_d.Contains(lookFor)) {
                                                        prio--;
                                                        string ur = FindHTML(_d, lookFor, "\"");
                                                        _d = RemoveOne(_d, lookFor);
                                                        string label = FindHTML(_d, "label:\"", "\"");
                                                        AddPotentialLink(episode, ur, "HD Upstream " + label, prio);
                                                    }
                                                }

                                                if (mightyupload != "") {
                                                    print("MIGHT" + mightyupload);
                                                    string baseUri = "http://mightyupload.com/" + mightyupload;
                                                    //string _d = DownloadString("http://mightyupload.com/" + mightyupload);
                                                    string post = "op=download1&usr_login=&id=" + FindHTML("|" + mightyupload, "|", "/") + "&fname=" + FindHTML(mightyupload, "/", ".html") + "&referer=&method_free=Free+Download+%3E%3E";

                                                    string _d = PostRequest(baseUri, baseUri, post, tempThred);//op=download1&usr_login=&id=k9on84m2bvr9&fname=tt0371746_play.mp4&referer=&method_free=Free+Download+%3E%3E
                                                    print("RESMIGHT:" + _d);
                                                    if (!GetThredActive(tempThred)) { return; };
                                                    string ur = FindHTML(_d, "<source src=\"", "\"");
                                                    AddPotentialLink(episode, ur, "HD MightyUpload", 16);
                                                }
                                                /*
                                                if (gogoStream.EndsWith(",&noneemb")) {
                                                    result = RemoveOne(result, ",&noneemb");
                                                    gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
                                                }


                                                gogoStream = gogoStream.Replace(",,&noneemb", "").Replace("\\", "");

                                                */

                                                Episode ep = activeMovie.episodes[episode];
                                                if (ep.links == null) {
                                                    activeMovie.episodes[episode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                                                }

                                                if (veryURL != "") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString("https://verystream.com/e/" + veryURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        // print(d);
                                                        debug("-------------------- HD --------------------");
                                                        url = "https://verystream.com/gettoken/" + FindHTML(d, "videolink\">", "<");
                                                        debug(url);
                                                        if (url != "https://verystream.com/gettoken/") {
                                                            /*
                                                            if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                // print(activeMovie.episodes[episode].Progress);
                                                                activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 10, name = "HD Verystream" });
                                                                linkAdded?.Invoke(null, 1);
                                                            }*/
                                                            AddPotentialLink(episode, url, "HD Verystream", 20);
                                                        }

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Verystream Link error (Read api)");
                                                    debug("");
                                                }
                                                //   activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                string __lookFor = "https:\\/\\/gomo.to\\/vid\\/";
                                                while (result.Contains(__lookFor)) {
                                                    string gogoStream = FindHTML(result, __lookFor, "\"");
                                                    result = RemoveOne(result, __lookFor);
                                                    if (gogoStream != "") {
                                                        debug(gogoStream);
                                                        try {
                                                            if (!GetThredActive(tempThred)) { return; };
                                                            string trueUrl = "https://" + GOMOURL + "/vid/" + gogoStream;
                                                            print(trueUrl);
                                                            d = client.DownloadString(trueUrl);
                                                            print("-->><<__" + d);
                                                            if (!GetThredActive(tempThred)) { return; };

                                                            //print(d);
                                                            // print("https://gomostream.com" + gogoStream);
                                                            //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
                                                            debug("-------------------- HD --------------------");
                                                            url = GetViduplayerUrl(d);
                                                            debug(url);
                                                            if (!url.EndsWith(".viduplayer.com/urlset/v.mp4") && !url.EndsWith(".viduplayer.com/vplayer/v.mp4") && !url.EndsWith(".viduplayer.com/adb/v.mp4")) {
                                                                /*if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                    //print(activeMovie.episodes[episode].Progress);
                                                                    activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 9, name = "HD Viduplayer" });
                                                                    linkAdded?.Invoke(null, 1);

                                                                }*/
                                                                debug("ADDED");
                                                                AddPotentialLink(episode, url, "HD Viduplayer", 19);

                                                            }
                                                            debug("--------------------------------------------");
                                                            debug("");
                                                        }
                                                        catch (System.Exception) {
                                                        }

                                                    }
                                                    else {
                                                        debug("HD Viduplayer Link error (Read api)");
                                                        debug("");
                                                    }
                                                }

                                                // activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                if (gunURL != "" && gunURL != "https://gounlimited.to/") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString(gunURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        string mid = FindHTML(d, "mp4|", "|");
                                                        string server = FindHTML(d, mid + "|", "|");
                                                        url = "https://" + server + ".gounlimited.to/" + mid + "/v.mp4";
                                                        if (mid != "" && server != "") {
                                                            /*
                                                            if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                // print(activeMovie.episodes[episode].Progress);

                                                                activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 8, name = "HD Go Unlimited" });
                                                                linkAdded?.Invoke(null, 1);

                                                            }*/
                                                            AddPotentialLink(episode, url, "HD Go Unlimited", 18);

                                                        }
                                                        debug("-------------------- HD --------------------");
                                                        debug(url);

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Go Link error (Read api)");
                                                    debug("");
                                                }
                                                // activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                if (onlyURL != "" && onlyURL != "https://onlystream.tv") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString(onlyURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        string _url = FindHTML(d, "file:\"", "\"");

                                                        if (_url == "") {
                                                            _url = FindHTML(d, "src: \"", "\"");
                                                        }

                                                        bool valid = false;
                                                        if (CheckIfURLIsValid(_url)) { // NEW USES JW PLAYER I THNIK, EASIER LINK EXTRACTION
                                                            url = _url; valid = true;
                                                        }
                                                        else { // OLD SYSTEM I THINK
                                                            string server = "";//FindHTML(d, "urlset|", "|");
                                                            string mid = FindHTML(d, "logo|", "|");

                                                            if (mid == "" || mid.Length < 10) {
                                                                mid = FindHTML(d, "mp4|", "|");
                                                            }

                                                            string prefix = FindHTML(d, "ostreamcdn|", "|");

                                                            url = "";
                                                            if (server != "") {
                                                                url = "https://" + prefix + ".ostreamcdn.com/" + server + "/" + mid + "/v/mp4"; // /index-v1-a1.m3u8 also works if you want the m3u8 file instead
                                                            }
                                                            else {
                                                                url = "https://" + prefix + ".ostreamcdn.com/" + mid + "/v/mp4";
                                                            }

                                                            if (mid != "" && prefix != "" && mid.Length > 10) {
                                                                valid = true;
                                                            }
                                                        }

                                                        if (valid) {
                                                            AddPotentialLink(episode, url, "HD Onlystream", 17);
                                                        }
                                                        else {
                                                            debug(d);
                                                            debug("FAILED URL: " + url);
                                                        }

                                                        debug("-------------------- HD --------------------");
                                                        debug(url);

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Only Link error (Read api)");
                                                    debug("");
                                                }

                                                done = true;
                                            }
                                            else {
                                                done = true;
                                                debug("DA FAILED");
                                            }
                                        }
                                        catch (Exception) {
                                            done = true;
                                        }

                                    }


                                }), _webRequest);


                            }), webRequest);
                        }
                        else {
                            debug("Dident get gogo");
                        }

                    }
                    catch (System.Exception) {
                        debug("Error");
                    }
                }
                finally {
                    while (!done) {
                        Thread.Sleep(20);
                    }
                    try {
                        linksProbablyDone?.Invoke(null, activeMovie.episodes[episode]);

                    }
                    catch (Exception) {

                    }
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "GomoSteam";
            tempThred.Thread.Start();
        }

        public static string PostRequest(string myUri, string referer = "", string _requestBody = "", TempThred? _tempThred = null)
        {
            try {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

                webRequest.Method = "POST";
                //  webRequest.Headers.Add("x-token", realXToken);
                webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                webRequest.Headers.Add("DNT", "1");
                webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
                webRequest.Headers.Add("TE", "Trailers");
                webRequest.Headers.Add("Pragma", "Trailers");
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Referer = referer;
                webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                // webRequest.Headers.Add("Host", "trollvid.net");
                webRequest.UserAgent = USERAGENT;
                webRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                bool done = false;
                string _res = "";
                webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
                    HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
                    Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

                    string requestBody = _requestBody;// --- RequestHeaders ---

                    byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

                    postStream.Write(byteArray, 0, byteArray.Length);
                    postStream.Close();

                    if (_tempThred != null) {
                        TempThred tempThred = (TempThred)_tempThred;
                        if (!GetThredActive(tempThred)) { return; }
                    }


                    // BEGIN RESPONSE

                    _webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
                        HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
                        if (_tempThred != null) {
                            TempThred tempThred = (TempThred)_tempThred;
                            if (!GetThredActive(tempThred)) { return; }
                        }
                        using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                            if (_tempThred != null) {
                                TempThred tempThred = (TempThred)_tempThred;
                                if (!GetThredActive(tempThred)) { return; }
                            }
                            _res = httpWebStreamReader.ReadToEnd();
                            done = true;
                        }
                    }), _webRequest);
                }), webRequest);


                for (int i = 0; i < 1000; i++) {
                    Thread.Sleep(10);
                    if (done) {
                        return _res;
                    }
                }
                return _res;
            }
            catch (Exception) {

                return "";
            }
        }

        /// <summary>
        /// Returns the true mx url of Viduplayer
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static string GetViduplayerUrl(string source)
        {
            source = source.Replace("||||", "|");
            source = source.Replace("|||", "|");
            source = source.Replace("||", "|");
            source = source.Replace("||", "|");
            source = source.Replace("||", "|");

            string inter = FindHTML(source, "|mp4|", "|");
            /*
            if(inter.Length < 5) {
                inter = FindHTML(_episode, "|srt|", "|");

            }
            if (inter.Length < 5) {
                inter = FindHTML(_episode, "|vvad|", "|");

            }*/

            string server = "";
            string _ser = FindHTML(source, "<img src=\"https://", ".viduplayer.com");
            if (_ser != "") {
                server = _ser;
            }

            if (server == "") {
                string[] serverStart = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
                for (int s = 0; s < serverStart.Length; s++) {
                    for (int i = 0; i < 100; i++) {
                        if (source.Contains("|" + serverStart[s] + i + "|")) {
                            server = serverStart[s] + i;
                        }
                    }
                }

                for (int i = 0; i < 100; i++) {
                    if (source.Contains("|www" + i + "|")) {
                        server = "www" + i;
                    }
                }
            }

            if (inter.Length < 5) {
                inter = FindReverseHTML(source, "|" + server + "|", "|");
            }
            if (inter == "adb") {
                inter = FindHTML(source, "|srt|", "|");
            }

            //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
            //100||vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fsxmxsx2ovpk34ua|1000
            if (inter.Length <= 5) {
                inter = FindHTML(source, "100|", "|1000");
            }
            if (server == "") {
                return "Error, server not found";
            }
            if (inter == "") {
                return "Error, index not found";
            }

            return "https://" + server + ".viduplayer.com/" + inter + "/v.mp4";
        }

        /// <summary>
        /// Do links contants inp
        /// </summary>
        /// <param name="links"></param>
        /// <param name="inp"></param>
        /// <returns></returns>
        public static bool LinkListContainsString(List<Link> links, string inp)
        {
            if (links == null) {
                return false;
            }
            else {
                try {
                    foreach (Link link in links) {
                        if (link.url == inp) { return true; }
                    }
                }
                catch {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Simple funct to download a sites fist page as string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="UTF8Encoding"></param>
        /// <returns></returns>
        public static string DownloadString(string url, TempThred? tempThred = null, bool UTF8Encoding = true, int repeats = 5)
        {
            string s = "";
            for (int i = 0; i < repeats; i++) {
                if (s == "") {
                    s = DownloadStringOnce(url, tempThred, UTF8Encoding);
                }
            }
            return s;
        }

        public static string DownloadStringOnce(string url, TempThred? tempThred = null, bool UTF8Encoding = true)
        {
            try {
                WebClient client = new WebClient();
                if (UTF8Encoding) {
                    client.Encoding = Encoding.UTF8; // TO GET SPECIAL CHARACTERS ECT
                }
                // ANDROID DOWNLOADSTRING

                bool done = false;
                string _s = "";
                bool error = false;
                client.DownloadStringCompleted += (o, e) => {
                    done = true;
                    if (!e.Cancelled) {
                        if (e.Error == null) {
                            _s = e.Result;
                        }
                        else {
                            _s = "";
                            error = true;
                            print(e.Error.Message + "|" + url);
                        }
                    }
                    else {
                        _s = "";
                    }
                };
                client.DownloadStringTaskAsync(url);
                for (int i = 0; i < 1000; i++) {
                    Thread.Sleep(10);
                    try {
                        if (tempThred != null) {
                            if (!GetThredActive((TempThred)tempThred)) {
                                client.CancelAsync();
                                return "";
                            }
                        }
                    }
                    catch (Exception) { }

                    if (done) {
                        //print(_s);
                        print(">>" + i);
                        return _s;
                    }
                }
                if (!error) {
                    client.CancelAsync();
                }
                return _s;

                // return client.DownloadString(url);
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// Makes first letter of all capital
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        static string ToTitle(string title)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(title.Replace("/", "").Replace("-", " "));
        }

        /// <summary>
        /// Used in while true loops to remove last used string
        /// </summary>
        /// <param name="d"></param>
        /// <param name="rem"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string RemoveOne(string d, string rem, int offset = 1)
        {
            int indexOfRem = d.IndexOf(rem);
            return d.Substring(indexOfRem + offset, d.Length - indexOfRem - offset);
        }

        /// <summary>
        /// Used to find string in string, for example 123>Hello<132123, hello can be found using FindHTML(d,">","<");
        /// </summary>
        /// <param name="all"></param>
        /// <param name="first"></param>
        /// <param name="end"></param>
        /// <param name="offset"></param>
        /// <param name="readToEndOfFile"></param>
        /// <returns></returns>
        public static string FindHTML(string all, string first, string end, int offset = 0, bool readToEndOfFile = false, bool decodeToNonHtml = false)
        {
            int firstIndex = all.IndexOf(first);
            if (firstIndex == -1) {
                return "";
            }
            int x = firstIndex + first.Length + offset;

            all = all.Substring(x, all.Length - x);
            int y = all.IndexOf(end);
            if (y == -1) {
                if (readToEndOfFile) {
                    y = all.Length;
                }
                else {
                    return "";
                }
            }
            //  print(x + "|" + y);

            string s = all.Substring(0, y);

            if (decodeToNonHtml) {
                return RemoveHtmlChars(s);
            }
            else {
                return s;
            }
        }
        public static void print(object o)
        {
#if DEBUG
            if (o != null) {
                System.Diagnostics.Debug.WriteLine(o.ToString());
            }
            else {
                System.Diagnostics.Debug.WriteLine("Null");
            }
#endif
        }
        public static void debug(object o)
        {
#if DEBUG
            if (o != null && DEBUG_WRITELINE) {
                System.Diagnostics.Debug.WriteLine(o.ToString());
            }
            else {
                System.Diagnostics.Debug.WriteLine("Null");
            }
#endif
        }

        // LICENSE
        //
        //   This software is dual-licensed to the public domain and under the following
        //   license: you are granted a perpetual, irrevocable license to copy, modify,
        //   publish, and distribute this file as you see fit.
        /// <summary>
        /// Does a fuzzy search for a pattern within a string.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern)
        {
            var patternIdx = 0;
            var strIdx = 0;
            var patternLength = pattern.Length;
            var strLength = stringToSearch.Length;

            while (patternIdx != patternLength && strIdx != strLength) {
                if (char.ToLower(pattern[patternIdx]) == char.ToLower(stringToSearch[strIdx]))
                    ++patternIdx;
                ++strIdx;
            }

            return patternLength != 0 && strLength != 0 && patternIdx == patternLength;
        }

        /// <summary>
        /// Does a fuzzy search for a pattern within a string, and gives the search a score on how well it matched.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <param name="outScore">The score which this search received, if a match was found.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern, out int outScore)
        {
            // Score consts
            const int adjacencyBonus = 5;               // bonus for adjacent matches
            const int separatorBonus = 10;              // bonus if match occurs after a separator
            const int camelBonus = 10;                  // bonus if match is uppercase and prev is lower

            const int leadingLetterPenalty = -3;        // penalty applied for every letter in stringToSearch before the first match
            const int maxLeadingLetterPenalty = -9;     // maximum penalty for leading letters
            const int unmatchedLetterPenalty = -1;      // penalty for every letter that doesn't matter


            // Loop variables
            var score = 0;
            var patternIdx = 0;
            var patternLength = pattern.Length;
            var strIdx = 0;
            var strLength = stringToSearch.Length;
            var prevMatched = false;
            var prevLower = false;
            var prevSeparator = true;                   // true if first letter match gets separator bonus

            // Use "best" matched letter if multiple string letters match the pattern
            char? bestLetter = null;
            char? bestLower = null;
            int? bestLetterIdx = null;
            var bestLetterScore = 0;

            var matchedIndices = new List<int>();

            // Loop over strings
            while (strIdx != strLength) {
                var patternChar = patternIdx != patternLength ? pattern[patternIdx] as char? : null;
                var strChar = stringToSearch[strIdx];

                var patternLower = patternChar != null ? char.ToLower((char)patternChar) as char? : null;
                var strLower = char.ToLower(strChar);
                var strUpper = char.ToUpper(strChar);

                var nextMatch = patternChar != null && patternLower == strLower;
                var rematch = bestLetter != null && bestLower == strLower;

                var advanced = nextMatch && bestLetter != null;
                var patternRepeat = bestLetter != null && patternChar != null && bestLower == patternLower;
                if (advanced || patternRepeat) {
                    score += bestLetterScore;
                    matchedIndices.Add((int)bestLetterIdx);
                    bestLetter = null;
                    bestLower = null;
                    bestLetterIdx = null;
                    bestLetterScore = 0;
                }

                if (nextMatch || rematch) {
                    var newScore = 0;

                    // Apply penalty for each letter before the first pattern match
                    // Note: Math.Max because penalties are negative values. So max is smallest penalty.
                    if (patternIdx == 0) {
                        var penalty = System.Math.Max(strIdx * leadingLetterPenalty, maxLeadingLetterPenalty);
                        score += penalty;
                    }

                    // Apply bonus for consecutive bonuses
                    if (prevMatched)
                        newScore += adjacencyBonus;

                    // Apply bonus for matches after a separator
                    if (prevSeparator)
                        newScore += separatorBonus;

                    // Apply bonus across camel case boundaries. Includes "clever" isLetter check.
                    if (prevLower && strChar == strUpper && strLower != strUpper)
                        newScore += camelBonus;

                    // Update pattern index IF the next pattern letter was matched
                    if (nextMatch)
                        ++patternIdx;

                    // Update best letter in stringToSearch which may be for a "next" letter or a "rematch"
                    if (newScore >= bestLetterScore) {
                        // Apply penalty for now skipped letter
                        if (bestLetter != null)
                            score += unmatchedLetterPenalty;

                        bestLetter = strChar;
                        bestLower = char.ToLower((char)bestLetter);
                        bestLetterIdx = strIdx;
                        bestLetterScore = newScore;
                    }

                    prevMatched = true;
                }
                else {
                    score += unmatchedLetterPenalty;
                    prevMatched = false;
                }

                // Includes "clever" isLetter check.
                prevLower = strChar == strLower && strLower != strUpper;
                prevSeparator = strChar == '_' || strChar == ' ';

                ++strIdx;
            }

            // Apply score for last match
            if (bestLetter != null) {
                score += bestLetterScore;
                matchedIndices.Add((int)bestLetterIdx);
            }

            outScore = score;
            return patternIdx == patternLength;
        }
    }
}
