﻿using Shared.resources;
using Shared.terrain;

namespace GameServer.realm.worlds.logic
{
    public class Test : World
    {
        private static ProtoWorld _testProto = new()
        {
            name = "Test World",
            sbName = "Test World",
            id = 0,
            setpiece = false,
            showDisplays = false,
            background = 0,
            blocking = 0,
            difficulty = 0,
            isLimbo = false,
            persist = false,
            portals = Empty<int>.Array,
            restrictTp = false,
            map = "",
            mapData = Empty<byte>.Array,
            // to-do: add test music
            music = new[] { "Test" }
        };

        public bool MapLoaded { get; private set; }

        public Test() : base(_testProto)
        {
        }

        protected override void Init()
        {
        }

        public void LoadMap(byte[] map) {
            if (!MapLoaded) {
                FromWorldMap(new MemoryStream(map));
                MapLoaded = true;
            }

            InitShops();
        }
    }
}