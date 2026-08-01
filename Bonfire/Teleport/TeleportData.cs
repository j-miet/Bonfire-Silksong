using System;
using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Static data for Teleport class.
    /// </summary>
    public static class TeleportData
    {
        // defines a bench location that has no map pin and must be added manually.
        public class SpecialLocation
        {
            public string Scene;           // PlayerData.tempRespawnScene
            public string Marker;          // PlayerData.tempRespawnMarker
            public int RespawnType;        // PlayerData.tempRespawnType
            public string AreaName;        // area name
            public string DefaultName;     // shown when custom names are off; use Scene here unless it's too long
            public string CustomName;      // shown when custom names are on
            public Func<bool> IsUnlocked;  // condition for whether to display this location
        }

        public static readonly Color32 DefaultMenuColor = Rgb(204, 191, 26);

        // Custom order for area names, in-game fast travel bench list then displays them in same order
        // BenchStaticLocations is the source of truth: if there isn't a valid map area under any of the
        // scenes, it will be ignored and falls to "Other" category instead.
        public static readonly List<(string areaName, Color32 color)> OrderedBenchAreaNames
            = new List<(string, Color32)>
        {
            ("Mosslands", Rgb(117, 160, 103)),
            ("Weavenest Atla", Rgb(109, 116, 151)),
            ("The Marrow", Rgb(120, 122, 120)),
            ("Wormways", Rgb(119, 106, 88)),
            ("Deep Docks", Rgb(192, 131, 66)),
            ("Hunter's March", Rgb(168, 94, 77)),
            ("Far Fields", Rgb(126, 137, 71)),
            ("Greymoor", Rgb(90, 123, 123)),
            ("Wisp Thicket", Rgb(68, 126, 81)),
            ("Bellhart", Rgb(238, 192, 128)),
            ("Sinner's Road", Rgb(187, 140, 118)),
            ("Bilewater", Rgb(157, 153, 73)),
            ("Putrified Ducts", Rgb(86, 120, 135)),
            ("Shellwood", Rgb(145, 151, 111)),
            ("Blasted Steps", Rgb(159, 132, 92)),
            ("Sands of Karak", Rgb(227, 134, 151)),
            ("The Slab", Rgb(122, 128, 138)),
            ("Mount Fay", Rgb(80, 108, 151)),
            ("The Citadel", Rgb(209, 194, 127)),
            ("Underworks", Rgb(99, 103, 109)),
            ("Memorium", Rgb(192, 178, 159)),
            ("The Cradle", Rgb(100, 106, 142)),
            ("Verdania", Rgb(96, 173, 120)),
            ("The Abyss", Rgb(94, 95, 94)),
            ("Other", DefaultMenuColor), // default option for uncategorized benches, never remove this
        };

        // Custom scene names for teleport locations with bench pins visible on map.
        // Key is the raw scene value from BenchTeleport.PrepareTeleportLocation
        //   => make sure to use final scene value for manually handled special cases!
        // Value is a tuple (custom name, map area). Custom name is optional: using empty string "" reverts back to
        // raw scene name. Map area is used in categorizing bench scenes.
        // If no key-value pair is found for specific scene, scene defaults to raw name and map area becomes "Other"
        //
        // Some benches don't have pins; these are filled manually in SpecialLocations list.
        public static readonly Dictionary<string, (string customName, string areaName)> BenchStaticLocations
            = new Dictionary<string, (string, string)>
            {
                {"Tut_03", ("Ruined Chapel", "Mosslands")},
                {"Tut_04", ("Ruined Chapel Interior", "Mosslands")},
                {"Bonetown", ("Bone Bottom", "Mosslands")},
                {"Mosstown_02c", ("Mosshome", "Mosslands")},

                {"Weave_07", ("Rest", "Weavenest Atla")},

                {"Bone_01c", ("Entrance", "The Marrow")},
                {"Bone_04", ("Crossing", "The Marrow")},
                {"Bone_10", ("Enclave", "The Marrow")},
                {"Bone_12", ("Cells", "The Marrow")},
                {"Bellshrine", ("Bellshrine", "The Marrow")},

                {"Crawl_08", ("Alchemist", "Wormways")},

                {"Dock_01", ("Entrance West", "Deep Docks")},
                {"Dock_06_Church", ("Ascent Top", "Deep Docks")},
                {"Dock_10", ("Sauna", "Deep Docks")},
                {"Bellshrine_05", ("Bellshrine", "Deep Docks")},
                {"Room_Forge", ("Forge", "Deep Docks")},
                {"Room_Diving_Bell", ("Diving Bell", "Deep Docks")},

                {"Ant_17", ("Rest", "Hunter's March")},

                {"Bone_East_10_Room", ("Pilgrim's Rest", "Far Fields")},
                {"Bone_East_15", ("Entrance West", "Far Fields")},
                {"Bone_East_27", ("Hideout", "Far Fields")},
                {"Bellway_03", ("Bellway", "Far Fields")},
                {"Bone_East_Umbrella", ("Seamstress", "Far Fields")},
                {"Bone_East_Weavehome", ("Weavenest Cindril", "Far Fields")},
                {"Sprintmaster_Cave", ("Sprintmaster Cave", "Far Fields")},

                {"Bellshrine_02", ("Bellshrine", "Greymoor")},
                {"Halfway_01", ("Halfway Home", "Greymoor")},
                {"Greymoor_02", ("Camp", "Greymoor")},
                {"Greymoor_08", ("Flea Caravan", "Greymoor")},

                {"Dust_10", ("Rest", "Sinner's Road")},
                {"Dust_11", ("Styx", "Sinner's Road")},

                {"Wisp_04", ("Rest", "Wisp Thicket")},

                {"Shadow_08", ("Hidden", "Bilewater")},
                {"Shadow_18", ("Bilehaven", "Bilewater")},
                {"Bellway_Shadow", ("Bellway", "Bilewater")},
                {"Organ_01", ("Exhaust Organ", "Bilewater")},
                {"Shadow_15", ("Trap", "Bilewater")},

                {"Aqueduct_05", ("Fleatopia", "Putrified Ducts")},
                {"Bellway_Aqueduct", ("Bellway", "Putrified Ducts")},
                {"Room_Huntress", ("Huntress", "Putrified Ducts")},

                {"Belltown", ("Town", "Bellhart")},

                {"Bellshrine_03", ("Bellshrine", "Shellwood")},
                {"Shellwood_01b", ("Room Center", "Shellwood")},
                {"Shellwood_08c", ("Entrance West", "Shellwood")},
                {"Mosstown_03", ("Entrance South", "Shellwood")},

                {"Bellway_08", ("Bellway", "Blasted Steps")},
                {"Coral_02", ("Entrance", "Blasted Steps")},
                {"Room_Pinstress", ("Pinstress", "Blasted Steps")},
                {"Coral_Judge_Arena", ("Flea Caravan", "Blasted Steps")},

                {"Coral_Tower_01", ("Coral Tower", "Sands of Karak")},
                {"Bellshrine_Coral", ("Room East", "Sands of Karak")},

                {"Slab_06", ("Bellway", "The Slab")},
                {"Slab_16", ("Room Northwest Alcove", "The Slab")},
                {"Slab_16_bench", ("Room Northwest", "The Slab")},
                {"Slab_20", ("Room East", "The Slab")},

                {"Peak_02", ("Entrance", "Mount Fay")},
                {"Peak_07", ("East Hidden", "Mount Fay")},
                {"Peak_12", ("Top Hidden", "Mount Fay")},
                {"Bellway_Peak", ("Shelter", "Mount Fay")},

                {"Song_10", ("Citadel Spa", "The Citadel")},
                {"Song_18", ("Room Center", "The Citadel")},
                {"Hang_01", ("Room West", "The Citadel")},
                {"Under_07b", ("Entrance Underworks", "The Citadel")},
                {"Bellway_City", ("Bellway", "The Citadel")},
                {"Song_Enclave", ("Songclave", "The Citadel")},
                {"Bellshrine_Enclave", ("First Shrine", "The Citadel")},
                {"Library_08", ("Vaultkeeper", "The Citadel")},
                {"Library_10", ("Vaults South", "The Citadel")},
                {"Cog_Bench", ("Cogwork Core", "The Citadel")},
                {"Ward_01", ("Whiteward", "The Citadel")},
                {"Hang_06b", ("High Halls", "The Citadel")},

                {"Under_01b", ("Entrance West", "Underworks")},
                {"Under_08", ("Confessional", "Underworks")},
                {"Under_17", ("The Cauldron", "Underworks")},

                {"Arborium_04", ("Rest", "Memorium")},

                {"Tube_Hub", ("Ventrica", "The Cradle")},

                {"Clover_20", ("Rest", "Verdania")},

                {"Abyss_09", ("Ascent", "The Abyss")},
                {"Abyss_12", ("Lake of Void", "The Abyss")},
                {"Room_Diving_Bell_Abyss", ("Diving Bell", "The Abyss")},
                {"Room_Diving_Bell_Abyss_Fixed", ("Diving Bell", "The Abyss")},
            };

        // bench locations without map pins.
        public static readonly List<SpecialLocation> SpecialLocations = new List<SpecialLocation>
        {
            // Bellhome
            new SpecialLocation
            {
                Scene = "Belltown_Room_Spare",
                Marker = "RestBench",
                RespawnType = 1,
                AreaName = "Bellhart",
                DefaultName = "Belltown_Room_Spare",
                CustomName = "Bellhome",
                IsUnlocked = () => PlayerData.instance.BelltownHouseUnlocked
                                && PlayerData.instance.BelltownHouseVisited
            },
            // Widow arena bellshrine
            new SpecialLocation
            {
                Scene = "Belltown_Shrine",
                Marker = "RestBench",
                RespawnType = 1,
                AreaName = "Bellhart",
                DefaultName = "Belltown_Shrine",
                CustomName = "Widow",
                IsUnlocked = () => !PlayerData.instance.blackThreadWorld // disabled in Act 3
                                && PlayerData.instance.hasNeedolin
            },
            // Surface climb
            new SpecialLocation
            {
                Scene = "Cradle_Destroyed_Challenge_Bench",
                Marker = "RestBench",
                RespawnType = 1,
                AreaName = "The Cradle",
                DefaultName = "Cradle_Destroyed_Challenge_Bench",
                CustomName = "Climb",
                IsUnlocked = () => PlayerData.instance.blackThreadWorld
                                && PlayerData.instance.hasSilkBossNeedle // available after picking Pale Nails
            },
        };

        // labels for bench menu styles
        public static readonly Dictionary<int, string> BenchMenuStyle = new Dictionary<int, string>
        {
            {0, "Basic + Default names"},
            {1, "Basic + Custom names"},
            {2, "Colored + Default names"},
            {3, "Colored + Custom names"}
        };


        private static Color32 Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    }
}