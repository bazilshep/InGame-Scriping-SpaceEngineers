using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using VRage.Game; // VRage.Game.dll
using System.Text;
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using Sandbox.Game.EntityComponents; // Sandbox.Game.dll
using VRage.Game.Components; // VRage.Game.dll
using VRage.Collections; // VRage.Library.dll
using VRage.Game.ObjectBuilders.Definitions; // VRage.Game.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll
using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

using SpaceEngineersIngameScript.Scripts;

namespace SpaceEngineersIngameScript.Scripts
{
    public class Program : MyGridProgram
    {

        #region "Ingame Script Text"


        /*
    Taleden's Inventory Manager
    version 1.5.3 (2016-12-14)

    "There are some who call me... TIM?"

    Steam Workshop: http://steamcommunity.com/sharedfiles/filedetails/?id=546825757
    User's Guide:   http://steamcommunity.com/sharedfiles/filedetails/?id=546909551


    LATEST CHANGES

    * "Gravel" in a block tag will now be understood to mean "Ingot/Stone", since
      that label may not be intuitive for new users.
    * Fixed a bug with TIM running on multiple grids docked via Connectors with
      mismatching DOCK labels; each such TIM should now continue to manage its own
      grid rather than one of them going dormant.
    * Fixed a bug with SPANning panels on small grids.

    */

        /* ***************************************************************************
        ******************************************************************************
        ******************************************************************************

        ADVANCED CONFIGURATION

        The settings below may be changed if you like, but read the notes and remember
        that any changes will be reverted the next time you update the script from the
        workshop.

        ******************************************************************************
        ******************************************************************************
        *************************************************************************** */

        // These settings now have script argument equivalents (described in the User's
        // Guide) which should be used instead.
        const int CYCLE_LENGTH = 1;
        const bool REWRITE_TAGS = true;
        const char TAG_OPEN = '[', TAG_CLOSE = ']';
        const string TAG_PREFIX = "TIM";
        const bool SCAN_COLLECTORS = false, SCAN_DRILLS = false, SCAN_GRINDERS = false, SCAN_WELDERS = false;
        const bool QUOTA_STABLE = true;

        // Managed assemblers will start and stop when the inventory of their
        // associated item reaches these multiples of its effective quota. Setting them
        // different from 1.0 prevents assemblers from rapidly switching on and off in
        // the case when the effective quota is rising due to ongoing production.
        // Defaults: 0.99 , 1.01
        const double ASSEMBLER_START = 0.99, ASSEMBLER_STOP = 1.01;

        // Equivalent to the respective Quota Panels, described in the User's Guide.
        // Defaults are based on the material requirements for several blueprints built
        // in to the game, plus a few popular blueprints published by the community.
        readonly Dictionary<string, Quota> DEFAULT_INGOT_QUOTAS = new Dictionary<string, Quota>
{
    { "COBALT",     new Quota(  50,  3.5f ) },
    { "GOLD",       new Quota(   5,  0.2f ) },
    { "IRON",       new Quota( 200, 88.0f ) },
    { "MAGNESIUM",  new Quota(   5,  0.1f ) },
    { "NICKEL",     new Quota(  30,  1.5f ) },
    { "PLATINUM",   new Quota(   5,  0.1f ) },
    { "SILICON",    new Quota(  50,  2.0f ) },
    { "SILVER",     new Quota(  20,  1.0f ) },
    { "STONE",      new Quota(  50,  2.5f ) },
    { "URANIUM",    new Quota(  10,  0.1f ) }
};
        readonly Dictionary<string, Quota> DEFAULT_COMPONENT_QUOTAS = new Dictionary<string, Quota>
{
    { "BULLETPROOFGLASS",   new Quota(  50,  2.0f ) },
    { "COMPUTER",           new Quota(  30,  5.0f ) },
    { "CONSTRUCTION",       new Quota( 150, 20.0f ) },
    { "DETECTOR",           new Quota(  10,  0.1f ) },
    { "DISPLAY",            new Quota(  10,  0.5f ) },
    { "EXPLOSIVES",         new Quota(   5,  0.1f ) },
    { "GIRDER",             new Quota(  10,  0.5f ) },
    { "GRAVITYGENERATOR",   new Quota(   1,  0.1f ) },
    { "INTERIORPLATE",      new Quota( 100, 10.0f ) },
    { "LARGETUBE",          new Quota(  10,  2.0f ) },
    { "MEDICAL",            new Quota(  15,  0.1f ) },
    { "METALGRID",          new Quota(  20,  2.0f ) },
    { "MOTOR",              new Quota(  20,  4.0f ) },
    { "POWERCELL",          new Quota(  20,  1.0f ) },
    { "RADIOCOMMUNICATION", new Quota(  10,  0.5f ) },
    { "REACTOR",            new Quota(  25,  2.0f ) },
    { "SMALLTUBE",          new Quota(  50,  3.0f ) },
    { "SOLARCELL",          new Quota(  20,  0.1f ) },
    { "STEELPLATE",         new Quota( 150, 40.0f ) },
    { "SUPERCONDUCTOR",     new Quota(  10,  1.0f ) },
    { "THRUST",             new Quota(  15,  5.0f ) }
};

        // Item types which may have quantities which are not whole numbers.
        // Defaults: INGOT, ORE
        readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> {
    "INGOT",
    "ORE",
};

        // Ore subtypes which refine into Ingots with a different subtype name, or
        // which cannot be refined at all (if set to "").
        // Defaults: ICE->"", ORGANIC->"", SCRAP->IRON
        readonly Dictionary<string, string> ORE_PRODUCT = new Dictionary<string, string> {
    { "ICE",     "" },
    { "ORGANIC", "" },
    { "SCRAP",   "IRON" },
};

        // Item types and subtypes that are restricted from being placed in some block
        // types and/or subtypes. Three-level Dictionary:
        //   Level 1: Block type (suffix of IMyCubeBlock.BlockDefinition.TypeIdString)
        //   Level 2: Block subtype (IMyCubeBlock.BlockDefinition.SubtypeId) or empty
        //   Level 3: Item type (suffix of IMyInventoryItem.Content.TypeId)
        //   Value: HashSet of item subtypes (IMyInventoryItem.Content.SubtypeId)
        // All block types must specify restrictions for the block subtype "", which
        // will apply to all subtypes of that block type which are not defined here.
        // The final value may be null, in which case all subtypes of the item type are
        // restricted.
        Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> BLOCK_RESTRICTIONS = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> {
    { "ASSEMBLER",  new Dictionary<string,Dictionary<string,HashSet<string>>> {
		// assemblers may also be tagged with what they can produce
		{ "",       new Dictionary<string,HashSet<string>> {
            { "ORE",    null },
        }},
    }},
    { "INTERIORTURRET", new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",           new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "MISSILE200MM","NATO_25X184MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "LARGEGATLINGTURRET", new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",               new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "MISSILE200MM","NATO_5P56X45MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "LARGEMISSILETURRET", new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",               new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "NATO_25X184MM","NATO_5P56X45MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "OXYGENGENERATOR",    new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",               new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",   null },
            { "COMPONENT",      null },
            { "INGOT",          null },
            { "ORE",            new HashSet<string> { "COBALT","GOLD","IRON","MAGNESIUM","NICKEL","ORGANIC","PLATINUM","SCRAP","SILICON","SILVER","STONE","URANIUM" } },
        }},
    }},
    { "OXYGENTANK",             new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",                   new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "PHYSICALGUNOBJECT",      null },
        }},
        { "LARGEHYDROGENTANK",  new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
        { "SMALLHYDROGENTANK",  new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "REACTOR",    new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",       new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  new HashSet<string> { "COBALT","GOLD","IRON","MAGNESIUM","NICKEL","PLATINUM","SCRAP","SILICON","SILVER","STONE" } },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "REFINERY",           new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",               new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null }, // technically refineries can take Ingot/Scrap "Old Scrap Metal" but I don't think that's obtainable any more
			{ "ORE",                    new HashSet<string> { "ICE","ORGANIC" } },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
        { "BLAST FURNACE",  new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           null },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null }, // see Scrap note above
			{ "ORE",                    new HashSet<string> { "GOLD","ICE","MAGNESIUM","ORGANIC","PLATINUM","SILICON","SILVER","STONE","URANIUM" } },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "SMALLGATLINGGUN",    new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",               new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "MISSILE200MM","NATO_5P56X45MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "SMALLMISSILELAUNCHER",   new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",                   new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "NATO_25X184MM","NATO_5P56X45MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
    { "SMALLMISSILELAUNCHERRELOAD", new Dictionary<string,Dictionary<string,HashSet<string>>> {
        { "",                       new Dictionary<string,HashSet<string>> {
            { "AMMOMAGAZINE",           new HashSet<string> { "NATO_25X184MM","NATO_5P56X45MM" } },
            { "COMPONENT",              null },
            { "GASCONTAINEROBJECT",     null },
            { "INGOT",                  null },
            { "ORE",                    null },
            { "OXYGENCONTAINEROBJECT",  null },
            { "PHYSICALGUNOBJECT",      null },
        }},
    }},
};


        /* ***************************************************************************
        ******************************************************************************
        ******************************************************************************

        SCRIPT INTERNALS

        Do not edit anything below unless you're sure you know what you're doing!

        ******************************************************************************
        ******************************************************************************
        *************************************************************************** */


        /*
        * DEFINITIONS
        */

        const byte VERS_MAJ = 2;
        const byte VERS_MIN = 0;
        const byte VERS_REV = 0;
        const string VERS_UPD = "2016-12-14-custom";

        const int MAX_CYCLE_STEPS = 11;
        char[] CHARS_WHITESPACE;
        char[] CHARS_COLON;
        char[] CHARS_NEWLINE;
        char[] CHARS_SPACECOMMA;
        readonly VRage.MyFixedPoint FIXEDPOINT_MAX_VALUE = (VRage.MyFixedPoint)9223372036854.775807;
        struct Quota { public int min; public float ratio; public Quota(int m, float r) { min = m; ratio = r; } }
        struct Pair { public int a, b; public Pair(int x, int y) { a = x; b = y; } }
        struct RunStats { public long num; public int step, cycle, time, xfers, refs, asms; }

        long numCalls = 0;
        int cycleLength = CYCLE_LENGTH;
        int cycleStep = 0;
        int numXfers, numRefs, numAsms;
        bool rewriteTags = REWRITE_TAGS;
        char tagOpen = TAG_OPEN;
        char tagClose = TAG_CLOSE;
        string tagPrefix = TAG_PREFIX;
        System.Text.RegularExpressions.Regex tagRegex = null;
        string panelFiller = "";
        bool foundNewItem = false;

        Dictionary<IMyCubeGrid, bool> gridDocked;
        List<string> types = new List<string>();
        Dictionary<string, string> typeLabel = new Dictionary<string, string>();
        Dictionary<string, List<string>> typeSubs = new Dictionary<string, List<string>>();
        Dictionary<string, Dictionary<string, string>> typeSubLabel = new Dictionary<string, Dictionary<string, string>>();
        List<string> subs = new List<string>();
        Dictionary<string, string> subLabel = new Dictionary<string, string>();
        Dictionary<string, List<string>> subTypes = new Dictionary<string, List<string>>();
        Dictionary<string, long> typeAmount = new Dictionary<string, long>();
        Dictionary<string, long> typeHidden = new Dictionary<string, long>();
        Dictionary<string, Dictionary<string, long>> typeSubAmount = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, long>> typeSubHidden = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, long>> typeSubAvail = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, long>> typeSubLocked = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>> typeSubInvenTotal = new Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>();
        Dictionary<string, Dictionary<string, Dictionary<IMyInventory, int>>> typeSubInvenSlot = new Dictionary<string, Dictionary<string, Dictionary<IMyInventory, int>>>();
        Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>> priTypeSubInvenRequest = new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>>();
        Dictionary<string, Dictionary<string, long>> typeSubMinimum = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, float>> typeSubRatio = new Dictionary<string, Dictionary<string, float>>();
        Dictionary<string, Dictionary<string, long>> typeSubQuota = new Dictionary<string, Dictionary<string, long>>();
        Dictionary<string, Dictionary<string, HashSet<IMyTerminalBlock>>> typeSubProducers = new Dictionary<string, Dictionary<string, HashSet<IMyTerminalBlock>>>();
        Dictionary<string, List<IMyTextPanel>> itypePanels = new Dictionary<string, List<IMyTextPanel>>();
        Dictionary<string, IMyTextPanel> qtypePanel = new Dictionary<string, IMyTextPanel>();
        Dictionary<string, List<string>> qtypeErrors = new Dictionary<string, List<string>>();
        List<IMyTextPanel> statusPanels = new List<IMyTextPanel>();
        RunStats[] statsLog = new RunStats[12];
        List<IMyTextPanel> debugPanels = new List<IMyTextPanel>();
        HashSet<string> debugLogic = new HashSet<string>();
        List<string> debugText = new List<string>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockTag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        HashSet<IMyInventory> invenLocked = new HashSet<IMyInventory>();
        HashSet<IMyInventory> invenHidden = new HashSet<IMyInventory>();
        Dictionary<IMyRefinery, string> refineryOre = new Dictionary<IMyRefinery, string>();
        Dictionary<IMyTextPanel, Pair> panelSpan = new Dictionary<IMyTextPanel, Pair>();
        Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> blockErrors = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>();


        /*
        * UTILITY FUNCTIONS
        */


        void RegisterTypeSubLabel(string itype, string isub, string label = null)
        {
            string itypelabel, isublabel;
            Quota q = new Quota();

            if (label == null)
                label = isub;
            itypelabel = itype;
            itype = itype.ToUpper();
            isublabel = isub;
            isub = isub.ToUpper();

            // new type?
            if (!typeSubs.ContainsKey(itype))
            {
                types.Add(itype);
                typeLabel[itype] = itypelabel;
                typeSubs[itype] = new List<string>();
                typeSubLabel[itype] = new Dictionary<string, string>();
                typeAmount[itype] = 0L;
                typeHidden[itype] = 0L;
                typeSubAmount[itype] = new Dictionary<string, long>();
                typeSubHidden[itype] = new Dictionary<string, long>();
                typeSubAvail[itype] = new Dictionary<string, long>();
                typeSubLocked[itype] = new Dictionary<string, long>();
                typeSubInvenTotal[itype] = new Dictionary<string, Dictionary<IMyInventory, long>>();
                typeSubInvenSlot[itype] = new Dictionary<string, Dictionary<IMyInventory, int>>();
                typeSubMinimum[itype] = new Dictionary<string, long>();
                typeSubRatio[itype] = new Dictionary<string, float>();
                typeSubQuota[itype] = new Dictionary<string, long>();
                typeSubProducers[itype] = new Dictionary<string, HashSet<IMyTerminalBlock>>();
                itypePanels[itype] = new List<IMyTextPanel>();
                qtypePanel[itype] = null;
                qtypeErrors[itype] = new List<string>();
            }

            // new subtype?
            if (!subTypes.ContainsKey(isub))
            {
                subs.Add(isub);
                subLabel[isub] = isublabel;
                subTypes[isub] = new List<string>();
            }

            // new type/subtype pair?
            if (!typeSubLabel[itype].ContainsKey(isub))
            {
                foundNewItem = true;
                typeSubs[itype].Add(isub);
                typeSubLabel[itype][isub] = label;
                subTypes[isub].Add(itype);
                typeSubAmount[itype][isub] = 0L;
                typeSubHidden[itype][isub] = 0L;
                typeSubAvail[itype][isub] = 0L;
                typeSubLocked[itype][isub] = 0L;
                typeSubInvenTotal[itype][isub] = new Dictionary<IMyInventory, long>();
                typeSubInvenSlot[itype][isub] = new Dictionary<IMyInventory, int>();
                if (itype == "INGOT" && DEFAULT_INGOT_QUOTAS.ContainsKey(isub))
                {
                    q = DEFAULT_INGOT_QUOTAS[isub];
                }
                else if (itype == "COMPONENT" && DEFAULT_COMPONENT_QUOTAS.ContainsKey(isub))
                {
                    q = DEFAULT_COMPONENT_QUOTAS[isub];
                }
                typeSubMinimum[itype][isub] = (long)((double)q.min * 1000000.0 + 0.5);
                typeSubRatio[itype][isub] = (q.ratio / 100.0f);
                typeSubQuota[itype][isub] = 0L;
                typeSubProducers[itype][isub] = new HashSet<IMyTerminalBlock>();
            }
        } // RegisterTypeSubLabel()


        bool BlockAcceptsTypeSub(IMyCubeBlock block, string itype, string isub)
        {
            string btype, bsub;

            btype = block.BlockDefinition.TypeIdString;
            btype = btype.Substring(btype.LastIndexOf('_') + 1).ToUpper();
            if (BLOCK_RESTRICTIONS.ContainsKey(btype) == false)
                return true;

            bsub = block.BlockDefinition.SubtypeId.ToUpper();
            if (BLOCK_RESTRICTIONS[btype].ContainsKey(bsub) == false)
                bsub = "";
            if (BLOCK_RESTRICTIONS[btype].ContainsKey(bsub) == false)
                return true;

            if (BLOCK_RESTRICTIONS[btype][bsub].ContainsKey(itype) == false)
                return true;
            if (BLOCK_RESTRICTIONS[btype][bsub][itype] == null)
                return false;
            return (BLOCK_RESTRICTIONS[btype][bsub][itype].Contains(isub) == false);
        } // BlockAcceptsTypeSub()


        HashSet<string> GetBlockAcceptedSubs(IMyCubeBlock block, string itype, HashSet<string> mysubs = null)
        {
            string btype, bsub;

            if (mysubs == null)
                mysubs = new HashSet<string>(typeSubs[itype]);
            btype = block.BlockDefinition.TypeIdString;
            btype = btype.Substring(btype.LastIndexOf('_') + 1).ToUpper();
            if (BLOCK_RESTRICTIONS.ContainsKey(btype))
            {
                bsub = block.BlockDefinition.SubtypeId.ToUpper();
                if (BLOCK_RESTRICTIONS[btype].ContainsKey(bsub) == false)
                    bsub = "";
                if (BLOCK_RESTRICTIONS[btype][bsub].ContainsKey(itype))
                {
                    if (BLOCK_RESTRICTIONS[btype][bsub][itype] == null)
                    {
                        mysubs.Clear();
                    }
                    else
                    {
                        mysubs.ExceptWith(BLOCK_RESTRICTIONS[btype][bsub][itype]);
                    }
                }
            }
            return mysubs;
        } // GetBlockAcceptedSubs()


        string GetBlockImpliedType(IMyCubeBlock block, string isub)
        {
            int t, found;
            string itype;

            found = 0;
            itype = null;
            t = subTypes[isub].Count;
            while (found < 2 & t-- > 0)
            {
                if (BlockAcceptsTypeSub(block, subTypes[isub][t], isub))
                {
                    found++;
                    itype = subTypes[isub][t];
                }
            }
            if (found != 1)
                return null;
            return itype;
        } // GetBlockImpliedType()


        void AddBlockRestriction(IMyCubeBlock block, string itype, string isub)
        {
            string btype, btypelabel, bsub, bsublabel;
            IEnumerator<string> enumStr;

            btypelabel = block.BlockDefinition.TypeIdString;
            btypelabel = btypelabel.Substring(btypelabel.LastIndexOf('_') + 1);
            btype = btypelabel.ToUpper();
            bsublabel = block.BlockDefinition.SubtypeId;
            bsub = bsublabel.ToUpper();

            if (BLOCK_RESTRICTIONS.ContainsKey(btype) == false)
            {
                BLOCK_RESTRICTIONS[btype] = new Dictionary<string, Dictionary<string, HashSet<string>>>();
                BLOCK_RESTRICTIONS[btype][""] = new Dictionary<string, HashSet<string>>();
            }

            if (BLOCK_RESTRICTIONS[btype].ContainsKey(bsub) == false)
            {
                BLOCK_RESTRICTIONS[btype][bsub] = new Dictionary<string, HashSet<string>>();
                enumStr = BLOCK_RESTRICTIONS[btype][""].Keys.GetEnumerator();
                while (enumStr.MoveNext())
                {
                    BLOCK_RESTRICTIONS[btype][bsub][enumStr.Current] = null;
                    if (BLOCK_RESTRICTIONS[btype][""][enumStr.Current] != null)
                        BLOCK_RESTRICTIONS[btype][bsub][enumStr.Current] = new HashSet<string>(BLOCK_RESTRICTIONS[btype][""][enumStr.Current]);
                }
            }

            if (BLOCK_RESTRICTIONS[btype][bsub].ContainsKey(itype) == false)
                BLOCK_RESTRICTIONS[btype][bsub][itype] = new HashSet<string>();

            if (BLOCK_RESTRICTIONS[btype][bsub][itype] != null)
            {
                if (BLOCK_RESTRICTIONS[btype][bsub][itype].Add(isub))
                    debugText.Add(btypelabel + "/" + bsublabel + " does not accept " + typeLabel[itype] + "/" + subLabel[isub]);
            }
        } // AddBlockRestriction()


        string GetShorthand(long amount)
        {
            long scale;
            if (amount <= 0L)
                return "0";
            if (amount < 10000L)
                return "< 0.01";
            if (amount >= 100000000000000L)
                return (amount / 1000000000000L).ToString() + " M";
            scale = (long)Math.Pow(10.0, Math.Floor(Math.Log10(amount)) - 2.0);
            amount = (long)((double)amount / scale + 0.5) * scale;
            if (amount < 1000000000L)
                return (amount / 1000000.0).ToString("0.##");
            if (amount < 1000000000000L)
                return (amount / 1000000000.0).ToString("0.##") + " K";
            return (amount / 1000000000000.0).ToString("0.##") + " M";
        } // GetShorthand()


        /*
        * GRID FUNCTIONS
        */

        void ScanGrids()
        {
            int b, a;
            List<IMyTerminalBlock> blocks;
            IMyShipConnector conn;
            System.Text.RegularExpressions.Match match;
            Dictionary<IMyShipConnector, HashSet<string>> connDocks;
            string[] attrs;
            List<string> fields;
            bool repeat, srcDocked, tgtDocked;

            // find all connectors and their dock tags (this is a stripped-down version
            // of the full tag parsing that must be done after scanning inventories, so
            // that any newly discovered items will be recognized in the tags)
            connDocks = new Dictionary<IMyShipConnector, HashSet<string>>();
            blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
            b = blocks.Count;
            while (b-- > 0)
            {
                conn = blocks[b] as IMyShipConnector;
                match = tagRegex.Match(blocks[b].CustomData);
                if (match.Success)
                {
                    connDocks[conn] = new HashSet<string>();
                    attrs = match.Groups[1].Captures[0].Value.Split(CHARS_SPACECOMMA, StringSplitOptions.RemoveEmptyEntries);
                    a = attrs.Length;
                    while (a-- > 0)
                    {
                        fields = new List<string>(attrs[a].ToUpper().Split(CHARS_COLON, StringSplitOptions.RemoveEmptyEntries));
                        if (fields.Count > 0 && fields[0] == "DOCK")
                        {
                            fields.RemoveAt(0);
                            connDocks[conn].UnionWith(fields);
                        }
                    }
                    if (connDocks[conn].Count < 1)
                        connDocks.Remove(conn);
                }
            }

            // determine which grids have valid connections
            gridDocked[Me.CubeGrid] = true;
            repeat = true;
            while (repeat)
            {
                repeat = false;
                b = blocks.Count;
                while (b-- > 0)
                {
                    conn = blocks[b] as IMyShipConnector;
                    if (gridDocked.TryGetValue(conn.CubeGrid, out srcDocked) & srcDocked == true & conn.IsConnected & conn.IsLocked & conn.OtherConnector != null)
                    {
                        if (gridDocked.TryGetValue(conn.OtherConnector.CubeGrid, out tgtDocked) == true & tgtDocked == true)
                        {
                            // other grid is already docked
                        }
                        else if (connDocks.ContainsKey(conn) == false & connDocks.ContainsKey(conn.OtherConnector) == false)
                        {
                            gridDocked[conn.OtherConnector.CubeGrid] = repeat = true;
                            debugText.Add("The grid containing " + conn.OtherConnector.CustomName + " is docked at " + conn.CustomName);
                        }
                        else if (connDocks.ContainsKey(conn) == false | connDocks.ContainsKey(conn.OtherConnector) == false)
                        {
                            // connection is invalid (one has dock tags, the other does not)
                            gridDocked[conn.OtherConnector.CubeGrid] = false;
                        }
                        else if (connDocks[conn].Overlaps(connDocks[conn.OtherConnector]) == true)
                        {
                            gridDocked[conn.OtherConnector.CubeGrid] = repeat = true;
                            debugText.Add("The grid containing " + conn.OtherConnector.CustomName + " is docked at " + conn.CustomName);
                        }
                        else
                        {
                            gridDocked[conn.OtherConnector.CubeGrid] = false;
                        }
                    }
                }
            }
        } // ScanGrids()


        /*
        * INVENTORY FUNCTIONS
        */


        void ScanBlocks<T>() where T : class
        {
            int b, i, s, n;
            string itype, isub;
            long amount, total;
            List<IMyTerminalBlock> blocks;
            IMyTerminalBlock block;
            System.Text.RegularExpressions.Match match;
            IMyInventory inven;
            List<IMyInventoryItem> stacks;

            // fetch all blocks of this type (on properly docked grids) and loop over them
            blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(blocks, (IMyTerminalBlock blk) => { return (gridDocked.ContainsKey(blk.CubeGrid) == false || gridDocked[blk.CubeGrid] == true); });
            b = blocks.Count;
            while (b-- > 0)
            {
                block = blocks[b];

                // check for a tag to come back and parse later, once we've seen all the items
                match = tagRegex.Match(block.CustomData);
                if (match.Success)
                    blockTag[block] = match;

                // check if any of this block's inventories are locked from reallocation
                if ((block is IMyFunctionalBlock) && ((block as IMyFunctionalBlock).Enabled & block.IsFunctional))
                {
                    // disabled and non-functional blocks may always be emptied
                    if ((block is IMyRefinery | block is IMyReactor | block is IMyOxygenGenerator) & match.Success == false)
                    {
                        // refineries, reactors and oxygen generators without tags may not have their first inventory (ores) emptied
                        invenLocked.Add(block.GetInventory(0));
                    }
                    else if (block is IMyAssembler && (block as IMyAssembler).IsQueueEmpty == false)
                    {
                        // assemblers with non-empty queues may not have their input inventory emptied (#1 if disassembling, #0 otherwise)
                        i = 0;
                        if ((block as IMyAssembler).DisassembleEnabled)
                            i = 1;
                        invenLocked.Add(block.GetInventory(i));
                    }
                }

                // loop over all inventories
                i = block.GetInventoryCount();
                while (i-- > 0)
                {
                    inven = block.GetInventory(i);

                    // loop over all stacks in this inventory
                    stacks = inven.GetItems();
                    s = stacks.Count;
                    while (s-- > 0)
                    {
                        // identify the stacked item
                        itype = stacks[s].Content.TypeId.ToString();
                        itype = itype.Substring(itype.LastIndexOf('_') + 1);
                        isub = stacks[s].Content.SubtypeId.ToString();

                        // new type or subtype?
                        RegisterTypeSubLabel(itype, isub, stacks[s].Content.SubtypeName);
                        itype = itype.ToUpper();
                        isub = isub.ToUpper();

                        // update amounts
                        amount = (long)((double)stacks[s].Amount * 1000000L);
                        typeAmount[itype] += amount;
                        typeSubAmount[itype][isub] += amount;
                        typeSubAvail[itype][isub] += amount;
                        typeSubInvenTotal[itype][isub].TryGetValue(inven, out total);
                        typeSubInvenTotal[itype][isub][inven] = total + amount;
                        typeSubInvenSlot[itype][isub].TryGetValue(inven, out n);
                        typeSubInvenSlot[itype][isub][inven] = Math.Max(n, s + 1);
                    }
                }
            }
        } // ScanBlocks()


        void AdjustAmounts()
        {
            int s;
            long amount;
            string itype, isub;
            IEnumerator<IMyInventory> enumInven;
            List<IMyInventoryItem> stacks;

            enumInven = invenHidden.GetEnumerator();
            while (enumInven.MoveNext())
            {
                stacks = enumInven.Current.GetItems();
                s = stacks.Count;
                while (s-- > 0)
                {
                    itype = stacks[s].Content.TypeId.ToString();
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[s].Content.SubtypeId.ToString().ToUpper();

                    amount = (long)((double)stacks[s].Amount * 1000000L);
                    typeAmount[itype] -= amount;
                    typeHidden[itype] += amount;
                    typeSubAmount[itype][isub] -= amount;
                    typeSubHidden[itype][isub] += amount;
                }
            }

            enumInven = invenLocked.GetEnumerator();
            while (enumInven.MoveNext())
            {
                stacks = enumInven.Current.GetItems();
                s = stacks.Count;
                while (s-- > 0)
                {
                    itype = stacks[s].Content.TypeId.ToString();
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[s].Content.SubtypeId.ToString().ToUpper();

                    amount = (long)((double)stacks[s].Amount * 1000000L);
                    typeSubAvail[itype][isub] -= amount;
                    typeSubLocked[itype][isub] += amount;
                }
            }
        } // AdjustAmounts()


        /*
        * TAG FUNCTIONS
        */


        void ParseBlockTags()
        {
            IEnumerator<IMyTerminalBlock> enumBlock;
            System.Text.RegularExpressions.Match match;
            IMyTerminalBlock block;
            IMyInventory inven;
            string blockname, attr, itype, isub;
            string[] attrs, fields;
            int a, i, priority, spanwide, spantall;
            long amount;
            float ratio;
            bool force, err;
            StringBuilder name;

            // loop over all blocks which have tags
            name = new StringBuilder();
            enumBlock = blockTag.Keys.GetEnumerator();
            while (enumBlock.MoveNext())
            {
                block = enumBlock.Current;
                match = blockTag[block];
                attrs = match.Groups[1].Captures[0].Value.Split(CHARS_SPACECOMMA, StringSplitOptions.RemoveEmptyEntries);
                blockname = block.CustomData;
                inven = block.GetInventory(0);

                // start building the canonical name
                name.Clear();
                name.Append(tagOpen);
                if (tagPrefix != "")
                    name.Append(tagPrefix + " ");

                // loop over all tag attributes
                for (a = 0; a < attrs.Length; a++)
                {
                    attr = attrs[a];
                    if (attr.Length >= 4 & "LOCKED".StartsWith(attr, StringComparison.OrdinalIgnoreCase))
                    {
                        i = block.GetInventoryCount();
                        while (i-- > 0)
                            invenLocked.Add(block.GetInventory(i));
                        name.Append("LOCKED ");
                    }
                    else if (attr.Equals("EXEMPT", StringComparison.OrdinalIgnoreCase))
                    { // for AIS compatibility
                        i = block.GetInventoryCount();
                        while (i-- > 0)
                            invenLocked.Add(block.GetInventory(i));
                        name.Append("EXEMPT ");
                    }
                    else if (attr.Equals("HIDDEN", StringComparison.OrdinalIgnoreCase))
                    {
                        i = block.GetInventoryCount();
                        while (i-- > 0)
                            invenHidden.Add(block.GetInventory(i));
                        name.Append("HIDDEN ");
                    }
                    else if ((block is IMyTextPanel) & attr.Length >= 4 & "STATUS".StartsWith(attr, StringComparison.OrdinalIgnoreCase))
                    {
                        statusPanels.Add(block as IMyTextPanel);
                        name.Append("STATUS ");
                    }
                    else if ((block is IMyTextPanel) & attr.Length >= 5 & "DEBUGGING".StartsWith(attr, StringComparison.OrdinalIgnoreCase))
                    {
                        debugPanels.Add(block as IMyTextPanel);
                        name.Append("DEBUG ");
                    }
                    else if ((block is IMyShipConnector) & (attr.Equals("DOCK", StringComparison.OrdinalIgnoreCase) | attr.StartsWith("DOCK:", StringComparison.OrdinalIgnoreCase)))
                    {
                        // already handled in ScanGrids(); checked here just for rewriting
                        name.Append("DOCK" + attr.Substring(4) + " ");
                    }
                    else if ((block is IMyTextPanel) & attr.StartsWith("SPAN:", StringComparison.OrdinalIgnoreCase))
                    {
                        fields = attr.Split(CHARS_COLON, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 3 && (int.TryParse(fields[1], out spanwide) & int.TryParse(fields[2], out spantall) & spanwide >= 1 & spantall >= 1))
                        {
                            panelSpan[block as IMyTextPanel] = new Pair(spanwide, spantall);
                            name.Append("SPAN:" + spanwide + ":" + spantall + " ");
                        }
                        else
                        {
                            name.Append(attr.ToLower() + " ");
                        }
                    }
                    else if ((a + 1) < attrs.Length && ((block is IMyTextPanel) & attr.ToUpper() == "THE" & attrs[a + 1].ToUpper() == "ENCHANTER"))
                    {
                        a++;
                        (block as IMyTextPanel).SetValueFloat("FontSize", 0.2f);
                        (block as IMyTextPanel).WritePublicTitle("TIM the Enchanter", false);
                        (block as IMyTextPanel).WritePublicText(panelFiller, false);
                        (block as IMyTextPanel).ShowPublicTextOnScreen();
                        name.Append("THE ENCHANTER ");
                    }
                    else if (ParseItemValueText(block, attr.ToUpper().Split(CHARS_COLON, StringSplitOptions.RemoveEmptyEntries), "", out itype, out isub, out priority, out amount, out ratio, out force))
                    {
                        err = false;
                        if (block is IMyTextPanel & priority <= 0)
                        {
                            itypePanels[itype].Add(block as IMyTextPanel);
                            amount = -1L;
                        }
                        else if (block is IMyTextPanel & priority > 0 & itype != "ORE")
                        {
                            if (qtypePanel[itype] == null)
                            {
                                qtypePanel[itype] = (block as IMyTextPanel);
                                amount = -1L;
                            }
                            else
                            {
                                debugText.Add("Cannot define more than one " + typeLabel[itype] + " Quota panel");
                                err = true;
                            }
                        }
                        else if (inven == null)
                        {
                            // can't store requests without an inventory
                            debugText.Add("Cannot define item requests for a " + block.DefinitionDisplayNameText);
                            err = true;
                        }
                        else if (isub == "")
                        {
                            if (block is IMyAssembler & itype != "INGOT" & itype != "ORE")
                            {
                                // assemblers can't have non-specific production assignments
                                debugText.Add("Assemblers must have a specific component assignment");
                                err = true;
                            }
                            else if (block is IMyRefinery & itype == "ORE")
                            {
                                // if a refinery requests non-specific ore, the priority and amount don't matter
                                refineryOre[block as IMyRefinery] = "";
                                priority = -1;
                                amount = -1L;
                            }
                            else
                            {
                                typeSubs[itype].ForEach((string s) => { AddInvenRequest(inven, itype, s, priority, amount); });
                            }
                        }
                        else if (block is IMyAssembler & itype != "INGOT" & itype != "ORE")
                        {
                            // specific assembler tags for types other than Ingot and Ore indicate production assignments
                            typeSubProducers[itype][isub].Add(block);
                            priority = -1;
                            amount = -1L;
                        }
                        else if (block is IMyRefinery & amount < 0L)
                        {
                            // if a refinery requests specific ore, an amount is required
                            AddInvenRequest(inven, itype, isub, priority, typeSubAvail[itype][isub]);
                        }
                        else
                        {
                            AddInvenRequest(inven, itype, isub, priority, amount);
                        }

                        // re-render the attribute in canonical form
                        if (rewriteTags)
                        {
                            if (err == true)
                            {
                                name.Append(attr.ToLower());
                            }
                            else
                            {
                                if (force == true)
                                {
                                    name.Append("FORCE:" + typeLabel[itype]);
                                    if (isub != "")
                                        name.Append("/" + subLabel[isub]);
                                }
                                else if (isub == "")
                                {
                                    name.Append(typeLabel[itype]);
                                }
                                else if (subTypes[isub].Count == 1 || GetBlockImpliedType(block, isub) == itype)
                                {
                                    name.Append(subLabel[isub]);
                                }
                                else
                                {
                                    name.Append(typeLabel[itype] + "/" + subLabel[isub]);
                                }
                                if (priority > 0 & priority < int.MaxValue)
                                {
                                    name.Append(":P" + priority);
                                }
                                if (amount >= 0L)
                                {
                                    name.Append(":" + (amount / 1e6));
                                }
                            }
                            name.Append(' ');
                        }
                    }
                    else
                    {
                        // if we couldn't parse the attribute, re-render it as-is for the user to correct
                        name.Append(attr.ToLower() + " ");
                        debugText.Add("Unrecognized, invalid or ambiguous tag rule: " + attr);
                    }
                }

                // finish building the normalized name and apply it to the block
                if (rewriteTags)
                {
                    if (name[name.Length - 1] == ' ')
                        name.Length--;
                    name.Append(tagClose).Append(blockname, match.Index + match.Length, blockname.Length - match.Index - match.Length);
                    block.CustomData  = name.ToString();
                }

                // check ownership
                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare)
                    debugText.Add("Cannot control \"" + block.CustomName + "\" due to differing ownership");
            }
        } // ParseBlockTags()


        void ParseQuotaPanels(bool quotaStable)
        {
            int l, x, y, wide, size, spanwide, spantall, height, priority;
            long amount, round;
            float ratio;
            bool force;
            string text, itype, isub;
            string[] empty;
            string[][] spanLines;
            IMyTextPanel panel, spanpanel;
            IMySlimBlock slim;
            Matrix matrix;
            StringBuilder sb;
            List<string> scalesubs = new List<string>();

            typeSubs["ORE"].ForEach((string qsub) => {
                typeSubMinimum["ORE"][qsub] = Math.Max(typeSubMinimum["ORE"][qsub], typeSubAmount["ORE"][qsub]);
                if (typeSubAmount["ORE"][qsub] == 0L)
                    typeSubMinimum["ORE"][qsub] = 0L;
            });

            empty = new string[1];
            empty[0] = " ";
            sb = new StringBuilder();

            types.ForEach((string qtype) => {
                panel = qtypePanel[qtype];
                if (panel != null)
                {
                    wide = size = 1;
                    if (panel.BlockDefinition.SubtypeId.EndsWith("Wide"))
                        wide = 2;
                    if (panel.BlockDefinition.SubtypeId.StartsWith("Small"))
                        size = 3;
                    spanwide = spantall = 1;
                    if (panelSpan.ContainsKey(panel))
                    {
                        spanwide = panelSpan[panel].a;
                        spantall = panelSpan[panel].b;
                    }
                    spanLines = new string[spanwide][];
                    matrix = new Matrix();
                    panel.Orientation.GetMatrix(out matrix);
                    for (y = 0; y < spantall; y++)
                    {
                        height = 0;
                        for (x = 0; x < spanwide; x++)
                        {
                            spanLines[x] = empty;
                            slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                            if (slim != null && slim.FatBlock is IMyTextPanel)
                            {
                                spanpanel = (slim.FatBlock as IMyTextPanel);
                                if (spanpanel.BlockDefinition.SubtypeId == panel.BlockDefinition.SubtypeId & spanpanel.GetPublicTitle().ToUpper().StartsWith(qtype + " QUOTAS"))
                                {
                                    spanLines[x] = spanpanel.GetPublicText().Split('\n');
                                    height = Math.Max(height, spanLines[x].Length);
                                }
                            }
                        }
                        for (l = 0; l < height; l++)
                        {
                            sb.Clear();
                            for (x = 0; x < spanwide; x++)
                            {
                                text = " ";
                                if (l < spanLines[x].Length)
                                    text = spanLines[x][l];
                                sb.Append(text);
                            }
                            text = sb.ToString().Trim(CHARS_WHITESPACE);
                            if (text == "" | text.StartsWith(qtype, StringComparison.OrdinalIgnoreCase))
                            {
                                // skip blank lines and the header
                            }
                            else if (ParseItemValueText(null, text.ToUpper().Split(CHARS_WHITESPACE, StringSplitOptions.RemoveEmptyEntries), qtype, out itype, out isub, out priority, out amount, out ratio, out force) & itype == qtype & isub != "")
                            {
                                typeSubMinimum[itype][isub] = Math.Max(0L, amount);
                                typeSubRatio[itype][isub] = Math.Max(0.0f, ratio);
                            }
                            else
                            {
                                qtypeErrors[qtype].Add(text);
                            }
                        }
                    }
                }

                round = 1L;
                if (FRACTIONAL_TYPES.Contains(qtype) == false)
                    round = 1000000L;
                if (quotaStable & typeAmount[qtype] > 0L)
                { // TODO
                    scalesubs.Clear();
                    typeSubs[qtype].ForEach((string qsub) => {
                        if (typeSubRatio[qtype][qsub] > 0.0f)
                            scalesubs.Add(qsub);
                    });
                    if (scalesubs.Count > 0)
                    {
                        scalesubs.Sort(delegate (string a, string b) { return (typeSubAmount[qtype][a] / typeSubRatio[qtype][a]).CompareTo(typeSubAmount[qtype][b] / typeSubRatio[qtype][b]); });
                        isub = scalesubs[scalesubs.Count / 2];
                        //Echo("median "+qtype+" is "+isub+", "+(typeAmount[qtype]/1e6)+" -> "+(typeSubAmount[qtype][isub] / typeSubRatio[qtype][isub] / 1e6));
                        typeAmount[qtype] = (long)(typeSubAmount[qtype][isub] / typeSubRatio[qtype][isub] + 0.5f);
                    }
                }
                typeSubs[qtype].ForEach((string qsub) => {
                    amount = Math.Max(typeSubQuota[qtype][qsub], Math.Max(typeSubMinimum[qtype][qsub], (long)(typeSubRatio[qtype][qsub] * typeAmount[qtype] + 0.5f)));
                    typeSubQuota[qtype][qsub] = (amount / round) * round;
                });
            });
        } // ParseQuotaPanels()


        bool ParseItemValueText(IMyCubeBlock block, string[] fields, string qtype, out string itype, out string isub, out int priority, out long amount, out float ratio, out bool force)
        {
            int f, l, t, s, found, mul;
            double val;
            string[] parts;
            HashSet<string> mysubs;
            IEnumerator<string> enumStr;

            found = 0;
            itype = "";
            isub = "";
            priority = -1;
            amount = -1L;
            ratio = -1.0f;
            force = false;

            // identify the item
            f = 0;
            if (fields[0].Trim() == "FORCE")
            {
                if (fields.Length == 1)
                    return false;
                force = true;
                f = 1;
            }
            parts = fields[f].Trim().Split('/');
            if (parts.Length >= 2)
            {
                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();
                if (typeSubs.ContainsKey(parts[0]) && (parts[1] == "" | typeSubLabel[parts[0]].ContainsKey(parts[1])))
                {
                    // exact type/subtype
                    if (force || BlockAcceptsTypeSub(block, parts[0], parts[1]))
                    {
                        found = 1;
                        itype = parts[0];
                        isub = parts[1];
                    }
                }
                else
                {
                    // search for a single accepted match
                    t = types.BinarySearch(parts[0]);
                    t = Math.Max(t, ~t);
                    while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                    {
                        s = typeSubs[types[t]].BinarySearch(parts[1]);
                        s = Math.Max(s, ~s);
                        while ((found < 2 & s < typeSubs[types[t]].Count) && typeSubs[types[t]][s].StartsWith(parts[1]))
                        {
                            if (force || BlockAcceptsTypeSub(block, types[t], typeSubs[types[t]][s]))
                            {
                                found++;
                                itype = types[t];
                                isub = typeSubs[types[t]][s];
                            }
                            s++;
                        }
                        // special case for gravel
                        if (found == 0 & types[t] == "INGOT" & "GRAVEL".StartsWith(parts[1]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                        {
                            found++;
                            itype = "INGOT";
                            isub = "STONE";
                        }
                        t++;
                    }
                }
            }
            else if (typeSubs.ContainsKey(parts[0]))
            {
                // exact type
                if (force || BlockAcceptsTypeSub(block, parts[0], ""))
                {
                    found++;
                    itype = parts[0];
                    isub = "";
                }
            }
            else if (subTypes.ContainsKey(parts[0]))
            {
                // exact subtype
                if (qtype != "" && typeSubLabel[qtype].ContainsKey(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = parts[0];
                }
                else
                {
                    t = subTypes[parts[0]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[parts[0]][t], parts[0]))
                        {
                            found++;
                            itype = subTypes[parts[0]][t];
                            isub = parts[0];
                        }
                    }
                }
            }
            else if (qtype != "")
            {
                // subtype of a known type
                s = typeSubs[qtype].BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < typeSubs[qtype].Count) && typeSubs[qtype][s].StartsWith(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = typeSubs[qtype][s];
                    s++;
                }
                // special case for gravel
                if (found == 0 & qtype == "INGOT" & "GRAVEL".StartsWith(parts[0]))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }
            else
            {
                // try it as a type
                t = types.BinarySearch(parts[0]);
                t = Math.Max(t, ~t);
                while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                {
                    if (force || BlockAcceptsTypeSub(block, types[t], ""))
                    {
                        found++;
                        itype = types[t];
                        isub = "";
                    }
                    t++;
                }
                // try it as a subtype
                s = subs.BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < subs.Count) && subs[s].StartsWith(parts[0]))
                {
                    t = subTypes[subs[s]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[subs[s]][t], subs[s]))
                        {
                            found++;
                            itype = subTypes[subs[s]][t];
                            isub = subs[s];
                        }
                    }
                    s++;
                }
                // special case for gravel
                if (found == 0 & "GRAVEL".StartsWith(parts[0]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }
            if (found != 1)
                return false;

            // if the subtype wasn't specified but the block only accepts one sub of this type, fill it in
            if (force == false & isub == "")
            {
                mysubs = GetBlockAcceptedSubs(block, itype);
                if (mysubs.Count == 1)
                {
                    // blarg, not allowed to use .First()
                    enumStr = mysubs.GetEnumerator();
                    enumStr.MoveNext();
                    isub = enumStr.Current;
                }
            }

            // parse the remaining fields
            while (++f < fields.Length)
            {
                fields[f] = fields[f].Trim();
                l = fields[f].Length;

                if (l == 0)
                {
                }
                else if (fields[f] == "IGNORE")
                {
                    amount = 0L;
                }
                else if (fields[f] == "OVERRIDE")
                {
                    // nothing to do; override is the default for same-priority requests on the same block
                }
                else if (fields[f] == "SPLIT")
                {
                    // nothing to do; split is the default for same-priority requests on different blocks
                }
                else if (fields[f][l - 1] == '%' & double.TryParse(fields[f].Substring(0, l - 1), out val))
                {
                    ratio = Math.Max(0.0f, (float)(val / 100.0));
                }
                else if ((fields[f][0] == 'P' | fields[f][0] == 'p') & double.TryParse(fields[f].Substring(1), out val))
                {
                    priority = Math.Max(1, (int)(val + 0.5));
                }
                else
                {
                    // check for numeric suffixes
                    mul = 1;
                    if (fields[f][l - 1] == 'K' | fields[f][l - 1] == 'k')
                    {
                        l--;
                        mul = 1000;
                    }
                    else if (fields[f][l - 1] == 'M' | fields[f][l - 1] == 'm')
                    {
                        l--;
                        mul = 1000000;
                    }

                    // try parsing the field as an amount value
                    if (double.TryParse(fields[f].Substring(0, l), out val))
                    {
                        amount = Math.Max(0L, (long)(val * mul * 1000000.0 + 0.5));
                    }
                }
            }

            return true;
        } // ParseItemValueText()


        void AddInvenRequest(IMyInventory inven, string itype, string isub, int priority, long amount)
        {
            long a;

            // undefined priority -> last priority
            if (priority < 0)
                priority = int.MaxValue;

            // new priority?
            if (!priTypeSubInvenRequest.ContainsKey(priority))
            {
                priTypeSubInvenRequest[priority] = new Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>();
            }

            // new type?
            if (!priTypeSubInvenRequest[priority].ContainsKey(itype))
            {
                priTypeSubInvenRequest[priority][itype] = new Dictionary<string, Dictionary<IMyInventory, long>>();
            }

            // new sub?
            if (!priTypeSubInvenRequest[priority][itype].ContainsKey(isub))
            {
                priTypeSubInvenRequest[priority][itype][isub] = new Dictionary<IMyInventory, long>();
            }

            // override?
            priTypeSubInvenRequest[priority][itype][isub].TryGetValue(inven, out a);
            typeSubQuota[itype][isub] -= Math.Max(0L, a);

            // set request
            priTypeSubInvenRequest[priority][itype][isub][inven] = amount;
            typeSubQuota[itype][isub] += Math.Max(0L, amount);

            // disable conveyors for certain blocks when we're managing their supply
            // (for some reason the Interior Turret doesn't have this option, even though it does have an inventory)
            if ((inven.Owner is IMyOxygenGenerator | inven.Owner is IMyReactor | inven.Owner is IMyRefinery | inven.Owner is IMyUserControllableGun) & ((inven.Owner is IMyLargeInteriorTurret) == false) & inven.Owner.UseConveyorSystem)
                (inven.Owner as IMyTerminalBlock).GetActionWithName("UseConveyor").Apply(inven.Owner as IMyTerminalBlock);
        } // AddInvenRequest()


        /*
        * TRANSFER FUNCTIONS
        */


        void AllocateItems(bool limited)
        {
            List<int> priorities;
            IEnumerator<string> enumType, enumSub;

            // establish priority order, adding 0 for refinery management
            priorities = new List<int>(priTypeSubInvenRequest.Keys);
            priorities.Sort();
            priorities.ForEach((int p) => {
                enumType = priTypeSubInvenRequest[p].Keys.GetEnumerator();
                while (enumType.MoveNext())
                {
                    enumSub = priTypeSubInvenRequest[p][enumType.Current].Keys.GetEnumerator();
                    while (enumSub.MoveNext())
                    {
                        AllocateItemBatch(limited, p, enumType.Current, enumSub.Current);
                    }
                }
            });

            // if we just finished the unlimited requests, check for leftovers
            if (limited == false)
            {
                types.ForEach((string itype) => {
                    typeSubs[itype].ForEach((string isub) => {
                        if (typeSubAvail[itype][isub] > 0L)
                            debugText.Add("No place to put " + GetShorthand(typeSubAvail[itype][isub]) + " " + typeLabel[itype] + "/" + subLabel[isub] + ", containers may be full");
                    });
                });
            }
        } // AllocateItems()


        void AllocateItemBatch(bool limited, int priority, string itype, string isub)
        {
            bool FUNC_DEBUG = debugLogic.Contains("sorting");
            int locked, dropped;
            long totalrequest, totalavail, request, avail, amount, moved, round;
            List<IMyInventory> invens;
            Dictionary<IMyInventory, long> invenRequest, invenTotal;
            IEnumerator<IMyInventory> enumReqInven, enumAmtInven;

            if (FUNC_DEBUG) debugText.Add("sorting " + typeLabel[itype] + "/" + subLabel[isub] + " lim=" + limited + " p=" + priority);

            round = 1L;
            if (FRACTIONAL_TYPES.Contains(itype) == false)
                round = 1000000L;
            invenRequest = new Dictionary<IMyInventory, long>();
            invenTotal = typeSubInvenTotal[itype][isub];

            // if none is available, there's nothing to do
            totalavail = typeSubAvail[itype][isub] + typeSubLocked[itype][isub];
            if (FUNC_DEBUG) debugText.Add("total avail=" + (totalavail / 1e6));
            if (totalavail <= 0L)
                return;

            // sum up the requests
            totalrequest = 0L;
            enumReqInven = priTypeSubInvenRequest[priority][itype][isub].Keys.GetEnumerator();
            while (enumReqInven.MoveNext())
            {
                request = priTypeSubInvenRequest[priority][itype][isub][enumReqInven.Current];
                if (request != 0L & limited == (request >= 0L))
                {
                    if (request < 0L)
                    {
                        request = 1000000L;
                        if (enumReqInven.Current.MaxVolume != FIXEDPOINT_MAX_VALUE)
                            request = (long)((double)enumReqInven.Current.MaxVolume * 1000000.0);
                    }
                    invenRequest[enumReqInven.Current] = request;
                    totalrequest += request;
                }
            }
            if (FUNC_DEBUG) debugText.Add("total req=" + (totalrequest / 1e6));
            if (totalrequest == 0L)
                return;

            // disqualify any locked invens which already have their share
            invens = new List<IMyInventory>(invenTotal.Keys);
            do
            {
                locked = 0;
                dropped = 0;
                enumAmtInven = invens.GetEnumerator();
                while (enumAmtInven.MoveNext())
                {
                    avail = invenTotal[enumAmtInven.Current];
                    if (avail > 0L & invenLocked.Contains(enumAmtInven.Current))
                    {
                        locked++;
                        invenRequest.TryGetValue(enumAmtInven.Current, out request);
                        amount = (long)((double)request / totalrequest * totalavail);
                        if (limited)
                            amount = Math.Min(amount, request);
                        amount = (amount / round) * round;

                        if (avail >= amount)
                        {
                            if (FUNC_DEBUG) debugText.Add("locked " + (enumAmtInven.Current.Owner as IMyTerminalBlock).CustomName + " gets " + (amount / 1e6) + ", has " + (avail / 1e6));
                            dropped++;
                            totalrequest -= request;
                            invenRequest[enumAmtInven.Current] = 0L;
                            totalavail -= avail;
                            typeSubLocked[itype][isub] -= avail;
                            invenTotal[enumAmtInven.Current] = 0L;
                        }
                    }
                }
            } while (locked > dropped & dropped > 0);

            // allocate the remaining available items
            enumReqInven = invenRequest.Keys.GetEnumerator();
            while (totalavail > 0 & enumReqInven.MoveNext())
            {
                // calculate this inven's allotment
                request = invenRequest[enumReqInven.Current];
                if (request <= 0L)
                    continue;
                amount = (long)((double)request / totalrequest * totalavail);
                if (limited)
                    amount = Math.Min(amount, request);
                amount = (amount / round) * round;
                if (FUNC_DEBUG) debugText.Add((enumReqInven.Current.Owner as IMyTerminalBlock).CustomName + " gets " + (request / 1e6) + " / " + (totalrequest / 1e6) + " of " + (totalavail / 1e6) + " = " + (amount / 1e6));
                totalrequest -= request;

                // check how much it already has
                if (invenTotal.TryGetValue(enumReqInven.Current, out avail))
                {
                    avail = Math.Min(avail, amount);
                    if (avail > 0L & itype == "ORE" & (enumReqInven.Current.Owner is IMyOxygenGenerator | enumReqInven.Current.Owner is IMyRefinery))
                        avail = TransferItem(itype, isub, avail, enumReqInven.Current, enumReqInven.Current);
                    amount -= avail;
                    totalavail -= avail;
                    if (invenLocked.Contains(enumReqInven.Current))
                    {
                        typeSubLocked[itype][isub] -= avail;
                    }
                    else
                    {
                        typeSubAvail[itype][isub] -= avail;
                    }
                    invenTotal[enumReqInven.Current] -= avail;
                }

                // get the rest from other unlocked invens
                // (if we ever move some but not all, then we're probably full)
                moved = 0L;
                enumAmtInven = invens.GetEnumerator();
                while (amount > 0L & (moved == 0L | moved == avail) & enumAmtInven.MoveNext())
                {
                    avail = Math.Min(invenTotal[enumAmtInven.Current], amount);
                    moved = 0L;
                    if (avail > 0L & invenLocked.Contains(enumAmtInven.Current) == false)
                    {
                        moved = TransferItem(itype, isub, avail, enumAmtInven.Current, enumReqInven.Current);
                        amount -= moved;
                        totalavail -= moved;
                        typeSubAvail[itype][isub] -= moved;
                        invenTotal[enumAmtInven.Current] -= moved;
                    }
                }
            }

            if (FUNC_DEBUG) debugText.Add("" + (totalavail / 1e6) + " left over");
        } // AllocateItemBatch()


        long TransferItem(string itype, string isub, long amount, IMyInventory fromInven, IMyInventory toInven)
        {
            List<IMyInventoryItem> stacks;
            int s;
            VRage.MyFixedPoint remaining, moved;
            uint id;
            int? index;
            string stype, ssub;

            remaining = (VRage.MyFixedPoint)(amount / 1000000.0);
            ssub = null;
            index = null;
            if ((toInven.Owner is IMyRefinery) & itype == "ORE")
                refineryOre.TryGetValue(toInven.Owner as IMyRefinery, out ssub); // if not found, ssub == null
            if (ssub == "")
                index = 0;
            stacks = fromInven.GetItems();
            s = Math.Min(typeSubInvenSlot[itype][isub][fromInven], stacks.Count);
            if (fromInven.Owner is IMyRefinery)
                s = stacks.Count;
            while (remaining > 0 & s-- > 0)
            {
                stype = stacks[s].Content.TypeId.ToString();
                stype = stype.Substring(stype.LastIndexOf('_') + 1).ToUpper();
                ssub = stacks[s].Content.SubtypeId.ToString().ToUpper();
                if (stype == itype && ssub == isub)
                {
                    moved = stacks[s].Amount;
                    id = stacks[s].ItemId;
                    if (fromInven == toInven & (s == index | index == null))
                    {
                        remaining -= moved;
                        if (remaining < 0)
                            remaining = 0;
                    }
                    else if (fromInven.TransferItemTo(toInven, s, index, true, remaining))
                    {
                        stacks = fromInven.GetItems();
                        if (s < stacks.Count && stacks[s].ItemId == id)
                            moved -= stacks[s].Amount;
                        remaining -= moved;
                        if (moved <= 0)
                        {
                            if ((double)toInven.CurrentVolume < (double)toInven.MaxVolume / 2)
                                AddBlockRestriction(toInven.Owner as IMyTerminalBlock, itype, isub);
                            s = 0;
                        }
                        else
                        {
                            numXfers++;
                            debugText.Add("Transferred " + GetShorthand((long)((double)moved * 1000000L)) + " " + typeLabel[itype] + "/" + subLabel[isub] + " from " + (fromInven.Owner as IMyTerminalBlock).CustomName + " to " + (toInven.Owner as IMyTerminalBlock).CustomName);
                        }
                    }
                    else if (fromInven.IsConnectedTo(toInven) == false)
                    {
                        if (blockErrors.ContainsKey(fromInven.Owner as IMyTerminalBlock) == false)
                            blockErrors[fromInven.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>();
                        blockErrors[fromInven.Owner as IMyTerminalBlock].Add(toInven.Owner as IMyTerminalBlock);
                        s = 0;
                    }
                }
            }

            amount -= (long)((double)remaining * 1000000.0 + 0.5);
            if (amount > 0L)
            {
                if (index == 0)
                {
                    refineryOre[toInven.Owner as IMyRefinery] = isub;
                    numRefs++;
                }
                if (itype == "ORE" & (toInven.Owner is IMyOxygenGenerator | toInven.Owner is IMyRefinery))
                {
                    typeSubProducers["ORE"][isub].Add(toInven.Owner as IMyTerminalBlock);
                    if (ORE_PRODUCT.TryGetValue(isub, out ssub) == false)
                        ssub = isub;
                    if (typeSubProducers["INGOT"].ContainsKey(ssub))
                        typeSubProducers["INGOT"][ssub].Add(toInven.Owner as IMyTerminalBlock);
                }
            }
            return amount;
        } // TransferItem()


        /*
        * MANAGEMENT FUNCTIONS
        */


        void ManageRefineries()
        {
            bool FUNC_DEBUG = debugLogic.Contains("refineries");
            bool found;
            int s, lo, hi, r;
            long amount, quota;
            string isubIngot, rtype, rtypelabel;
            Dictionary<string, double> oreWeight;
            List<string> ores;
            IEnumerator<IMyRefinery> enumRef;
            Dictionary<string, HashSet<string>> rtypeOres;
            Dictionary<string, Queue<IMyRefinery>> rtypeBlocks;
            List<string> rtypes;

            // if we've never seen ore, there's certainly nothing to do here
            if (typeSubs.ContainsKey("ORE") == false)
                return;

            // find available ores
            oreWeight = new Dictionary<string, double>();
            typeSubs["ORE"].ForEach((string isub) => {
                if (ORE_PRODUCT.TryGetValue(isub, out isubIngot) == false)
                    isubIngot = isub;
                if (isubIngot != "" & typeSubAvail["ORE"][isub] > 0L)
                {
                    // assign priority weight by ingot quota status
                    amount = 0L;
                    quota = 1L;
                    if (typeSubs.ContainsKey("INGOT") && typeSubAmount["INGOT"].ContainsKey(isubIngot))
                    {
                        amount = typeSubAmount["INGOT"][isubIngot];
                        quota = typeSubQuota["INGOT"][isubIngot];
                    }
                    oreWeight[isub] = double.MaxValue;
                    if (quota > 0L)
                        oreWeight[isub] = (double)(amount - quota) / quota;
                    if (FUNC_DEBUG) debugText.Add("refining Ore/" + subLabel[isub] + " priority " + (amount / 1e6) + "/" + (quota / 1e6) + "=" + oreWeight[isub]);
                }
            });
            if (oreWeight.Count == 0)
                return;

            // find available refineries
            rtypeOres = new Dictionary<string, HashSet<string>>();
            rtypeBlocks = new Dictionary<string, Queue<IMyRefinery>>();
            enumRef = refineryOre.Keys.GetEnumerator();
            while (enumRef.MoveNext())
            {
                if ((false /*TODO testing*/ | enumRef.Current.Enabled) & refineryOre[enumRef.Current] == "")
                {
                    rtypelabel = enumRef.Current.BlockDefinition.SubtypeId;
                    rtype = rtypelabel.ToUpper();
                    if (rtypeOres.ContainsKey(rtype) == false)
                    {
                        rtypeOres[rtype] = GetBlockAcceptedSubs(enumRef.Current, "ORE", new HashSet<string>(oreWeight.Keys));
                        if (FUNC_DEBUG) debugText.Add("Refinery/" + rtypelabel + " accepts " + rtypeOres[rtype].Count + " available ore(s)");
                        if (rtypeOres[rtype].Count > 0)
                            rtypeBlocks[rtype] = new Queue<IMyRefinery>();
                    }
                    if (rtypeBlocks.ContainsKey(rtype))
                        rtypeBlocks[rtype].Enqueue(enumRef.Current);
                }
            }
            if (rtypeBlocks.Count == 0)
                return;

            // prioritize ores and refinery types
            ores = new List<string>(oreWeight.Keys);
            ores.Sort(delegate (string a, string b) { return oreWeight[a].CompareTo(oreWeight[b]); });
            lo = 0;
            hi = 0;
            while (hi < ores.Count && oreWeight[ores[hi]] < 0.0)
                hi++;
            rtypes = new List<string>(rtypeBlocks.Keys);
            rtypes.Sort(delegate (string a, string b) { return rtypeOres[a].Count.CompareTo(rtypeOres[b].Count); });

            // allocate under-budget ores, then over-budget ores
            while (lo < ores.Count)
            {
                if (lo < hi)
                {
                    do
                    {
                        found = false;
                        for (s = lo; s < hi; s++)
                        {
                            for (r = 0; r < rtypes.Count; r++)
                            {
                                rtype = rtypes[r];
                                if (rtypeBlocks[rtype].Count > 0 & rtypeOres[rtype].Contains(ores[s]))
                                {
                                    found = true;
                                    if (FUNC_DEBUG) debugText.Add("assigned " + rtypeBlocks[rtype].Peek().CustomName + " to refine " + subLabel[ores[s]]);
                                    AddInvenRequest(rtypeBlocks[rtype].Dequeue().GetInventory(0), "ORE", ores[s], 0, -1L);
                                    r = rtypes.Count;
                                }
                            }
                        }
                    } while (found);
                }
                lo = hi;
                hi = ores.Count;
            }
        } // ManageRefineries()


        void ManageAssemblers()
        {
            bool FUNC_DEBUG = debugLogic.Contains("assemblers");
            double ratio;
            IEnumerator<IMyTerminalBlock> enumProd;
            HashSet<IMyTerminalBlock> disabledProd;
            IMyAssembler assembler;

            disabledProd = new HashSet<IMyTerminalBlock>();
            types.ForEach((string itype) => {
                if (itype != "INGOT" & itype != "ORE")
                {
                    typeSubs[itype].ForEach((string isub) => {
                        ratio = (double)typeSubAmount[itype][isub] / typeSubQuota[itype][isub];
                        if (FUNC_DEBUG) debugText.Add("assembling " + typeLabel[itype] + "/" + subLabel[isub] + " priority " + (typeSubAmount[itype][isub] / 1e6) + "/" + (typeSubQuota[itype][isub] / 1e6) + "=" + ratio);
                        disabledProd.Clear();

                        enumProd = typeSubProducers[itype][isub].GetEnumerator();
                        while (enumProd.MoveNext())
                        {
                            assembler = (enumProd.Current as IMyAssembler);
                            if (assembler == null)
                            {
                                debugText.Add(typeLabel[itype] + "/" + subLabel[isub] + " has non-assembler producer " + enumProd.Current.CustomName);
                                disabledProd.Add(enumProd.Current);
                            }
                            else if (assembler.Enabled)
                            {
                                if (ratio >= ASSEMBLER_STOP)
                                {
                                    assembler.ApplyAction("OnOff_Off");
                                    disabledProd.Add(enumProd.Current);
                                    if (FUNC_DEBUG) debugText.Add(assembler.CustomName + " switched off");
                                }
                                else if (FUNC_DEBUG) debugText.Add(assembler.CustomName + " remains on");
                            }
                            else if (ratio < ASSEMBLER_START)
                            {
                                assembler.ApplyAction("OnOff_On");
                                if (FUNC_DEBUG) debugText.Add(assembler.CustomName + " switched on");
                            }
                            else
                            {
                                disabledProd.Add(enumProd.Current);
                                if (FUNC_DEBUG) debugText.Add(assembler.CustomName + " remains off");
                            }
                        }
                        typeSubProducers[itype][isub].ExceptWith(disabledProd);
                        numAsms += typeSubProducers[itype][isub].Count;
                    });
                }
            });
        } // ManageAssemblers()


        /*
        * PANEL DISPLAYS
        */


        void UpdateQuotaPanels()
        {
            string errors;
            ScreenFormatter sf;

            types.ForEach((string qtype) => {
                if (qtype != "ORE" & qtypePanel[qtype] != null)
                {
                    sf = new ScreenFormatter(3);
                    sf.SetAlign(1, 1);
                    sf.SetAlign(2, 1);
                    sf.Append(0, typeLabel[qtype], true);
                    sf.Append(1, "  MinQty", true);
                    sf.Append(2, "  MinPct", true);
                    sf.Append(0, "");
                    sf.Append(1, "");
                    sf.Append(2, "");
                    typeSubs[qtype].ForEach((string isub) => {
                        sf.Append(0, typeSubLabel[qtype][isub], true);
                        sf.Append(1, Math.Round(typeSubMinimum[qtype][isub] / 1000000.0, 2).ToString(), true);
                        sf.Append(2, Math.Round(typeSubRatio[qtype][isub] * 100.0f, 2).ToString() + "%", true);
                    });
                    errors = "";
                    if (qtypeErrors[qtype].Count > 0)
                        errors = "\n\n" + String.Join("\n", qtypeErrors[qtype]).ToLower();
                    WriteTableToPanel(typeLabel[qtype] + " Quotas", sf, qtypePanel[qtype], true, errors);
                }
            });
        } // UpdateQuotaPanels()


        void UpdateInventoryPanels()
        {
            int b;
            long maxamount, maxquota;
            string header2, header5, text;
            ScreenFormatter sf;

            types.ForEach((string itype) => {
                header2 = " Asm ";
                header5 = "Quota";
                if (itype == "INGOT")
                {
                    header2 = " Ref ";
                }
                else if (itype == "ORE")
                {
                    header2 = " Ref ";
                    header5 = "Max";
                }

                if (itypePanels[itype].Count > 0)
                {
                    sf = new ScreenFormatter(6);
                    sf.SetBar(0);
                    sf.SetFill(0, 1);
                    sf.SetAlign(2, 1);
                    sf.SetAlign(3, 1);
                    sf.SetAlign(4, 1);
                    sf.SetAlign(5, 1);
                    sf.Append(0, "");
                    sf.Append(1, typeLabel[itype], true);
                    sf.Append(2, header2, true);
                    sf.Append(3, "Qty", true);
                    sf.Append(4, " / ", true);
                    sf.Append(5, header5, true);
                    sf.Append(0, "");
                    sf.Append(1, "");
                    sf.Append(2, "");
                    sf.Append(3, "");
                    sf.Append(4, "");
                    sf.Append(5, "");
                    maxamount = 0;
                    maxquota = 0;
                    typeSubs[itype].ForEach((string isub) => {
                        sf.Append(0, (typeSubAmount[itype][isub] == 0L) ? "0.0" : ((double)typeSubAmount[itype][isub] / typeSubQuota[itype][isub]).ToString());
                        sf.Append(1, typeSubLabel[itype][isub], true);
                        text = "";
                        if (typeSubProducers[itype][isub].Count > 0)
                            text = typeSubProducers[itype][isub].Count + "  ";
                        sf.Append(2, text);
                        sf.Append(3, GetShorthand(typeSubAmount[itype][isub]));
                        sf.Append(4, " / ", true);
                        sf.Append(5, GetShorthand(typeSubQuota[itype][isub]));
                        maxamount = Math.Max(maxamount, typeSubAmount[itype][isub]);
                        maxquota = Math.Max(maxquota, typeSubQuota[itype][isub]);
                    });
                    text = "8.88";
                    if (maxamount >= 1000000000L)
                        text = "8.88 K";
                    if (maxamount >= 1000000000000L)
                        text = "8.88 M";
                    sf.SetWidth(3, ScreenFormatter.GetWidth(text, true));
                    text = "8.88";
                    if (maxquota >= 1000000000L)
                        text = "8.88 K";
                    if (maxquota >= 1000000000000L)
                        text = "8.88 M";
                    sf.SetWidth(5, ScreenFormatter.GetWidth(text, true));

                    for (b = 0; b < itypePanels[itype].Count; b++)
                    {
                        WriteTableToPanel(typeLabel[itype] + " Inventory", sf, itypePanels[itype][b], true);
                    }
                }
            });
        } // UpdateInventoryPanels()


        void UpdateStatusPanels()
        {
            int b, unused;
            long r;
            IEnumerator<IMyTerminalBlock> enumFrom, enumTo;
            RunStats stats;
            StringBuilder sb;
            IMyTextPanel panel;

            if (statusPanels.Count > 0)
            {
                sb = new StringBuilder();
                sb.Append("Taleden's Inventory Manager\n");
                sb.Append("v" + VERS_MAJ + "." + VERS_MIN + "." + VERS_REV + " (" + VERS_UPD + ")\n\n");
                sb.Append(ScreenFormatter.Format("Run", 80, out unused, 1, true));
                sb.Append(ScreenFormatter.Format("Step", 110 + unused, out unused, 1, true));
                sb.Append(ScreenFormatter.Format("Time", 130 + unused, out unused, 1, true));
                sb.Append(ScreenFormatter.Format("XFers", 110 + unused, out unused, 1, true));
                sb.Append(ScreenFormatter.Format("Refs", 110 + unused, out unused, 1, true));
                sb.Append(ScreenFormatter.Format("Asms", 110 + unused, out unused, 1, true));
                sb.Append("\n\n");
                for (r = Math.Max(1, numCalls - statsLog.Length + 1); r <= numCalls; r++)
                {
                    stats = statsLog[r % statsLog.Length];
                    unused = 0;
                    sb.Append(ScreenFormatter.Format(stats.num.ToString(), 80, out unused, 1));
                    sb.Append(ScreenFormatter.Format("" + stats.step + " / " + stats.cycle, 110 + unused, out unused, 1, true));
                    sb.Append(ScreenFormatter.Format(stats.time + " ms", 130 + unused, out unused, 1));
                    sb.Append(ScreenFormatter.Format(stats.xfers.ToString(), 110 + unused, out unused, 1));
                    sb.Append(ScreenFormatter.Format(stats.refs.ToString(), 110 + unused, out unused, 1));
                    sb.Append(ScreenFormatter.Format(stats.asms.ToString(), 110 + unused, out unused, 1));
                    sb.Append("\n");
                }

                // write to the panels
                for (b = 0; b < statusPanels.Count; b++)
                {
                    panel = statusPanels[b];
                    panel.WritePublicTitle("Script Status", false);
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Status panels cannot be spanned");
                    panel.WritePublicText(sb.ToString(), false);
                    panel.ShowPublicTextOnScreen();
                }
            }

            if (debugPanels.Count > 0)
            {
                enumFrom = blockErrors.Keys.GetEnumerator();
                while (enumFrom.MoveNext())
                {
                    enumTo = blockErrors[enumFrom.Current].GetEnumerator();
                    while (enumTo.MoveNext())
                        debugText.Add("No conveyor connection from " + enumFrom.Current.CustomName + " to " + enumTo.Current.CustomName);
                }
                for (b = 0; b < debugPanels.Count; b++)
                {
                    panel = debugPanels[b];
                    panel.WritePublicTitle("Script Debugging", false);
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Debug panels cannot be spanned");
                    panel.WritePublicText(String.Join("\n", debugText), false);
                    panel.ShowPublicTextOnScreen();
                }
            }
            blockErrors.Clear();
        } // UpdateStatusPanels()


        void WriteTableToPanel(string title, ScreenFormatter sf, IMyTextPanel panel, bool allowspan = true, string postscript = "")
        {
            int spanwide, spantall, rows, wide, size, width, height;
            int x, y, r;
            float fontsize;
            string[][] spanLines;
            string text;
            Matrix matrix;
            IMySlimBlock slim;
            IMyTextPanel spanpanel;

            // get the spanning dimensions, if any
            wide = 1;
            if (panel.BlockDefinition.SubtypeId.EndsWith("Wide"))
                wide = 2;
            size = 1;
            if (panel.BlockDefinition.SubtypeId.StartsWith("Small"))
                size = 3;
            spanwide = spantall = 1;
            if (allowspan & panelSpan.ContainsKey(panel))
            {
                spanwide = panelSpan[panel].a;
                spantall = panelSpan[panel].b;
            }

            // reduce font size to fit everything
            x = sf.GetMinWidth();
            x = (x / spanwide) + ((x % spanwide > 0) ? 1 : 0);
            y = sf.GetNumRows();
            y = (y / spantall) + ((y % spantall > 0) ? 1 : 0);
            width = 658 * wide;
            fontsize = panel.GetValueFloat("FontSize");
            if (fontsize < 0.25f)
                fontsize = 1.0f;
            if (x > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(width * 10 / x) / 10.0f));
            if (y > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(176 / y) / 10.0f));

            // calculate how much space is available on each panel
            width = (int)((float)width / fontsize);
            height = (int)(17.6f / fontsize);

            // write to each panel
            if (spanwide > 1 | spantall > 1)
            {
                spanLines = sf.ToSpan(width, spanwide);
                matrix = new Matrix();
                panel.Orientation.GetMatrix(out matrix);
                for (x = 0; x < spanwide; x++)
                {
                    r = 0;
                    for (y = 0; y < spantall; y++)
                    {
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        if (slim != null && (slim.FatBlock is IMyTextPanel) && slim.FatBlock.BlockDefinition.SubtypeId == panel.BlockDefinition.SubtypeId)
                        {
                            spanpanel = slim.FatBlock as IMyTextPanel;
                            rows = Math.Max(0, spanLines[x].Length - r);
                            if (y + 1 < spantall)
                                rows = Math.Min(rows, height);
                            text = "";
                            if (r < spanLines[x].Length)
                                text = String.Join("\n", spanLines[x], r, rows);
                            if (x == 0 & (y + 1) == spantall & postscript != "")
                                text += postscript;
                            spanpanel.SetValueFloat("FontSize", fontsize);
                            spanpanel.WritePublicTitle(title + " (" + (x + 1) + "," + (y + 1) + ")", false);
                            spanpanel.WritePublicText(text, false);
                            spanpanel.ShowPublicTextOnScreen();
                        }
                        r += height;
                    }
                }
            }
            else
            {
                panel.SetValueFloat("FontSize", fontsize);
                panel.WritePublicTitle(title, false);
                panel.WritePublicText(sf.ToString(width) + postscript, false);
                panel.ShowPublicTextOnScreen();
            }
        } // WriteTableToPanel()


        /*
        * MAIN
        */


        void Main(string argument)
        {
            DateTime dtStart = DateTime.Now;
            int i, j, argCycle, runtime;
            bool argRewriteTags, argScanCollectors, argScanDrills, argScanGrinders, argScanWelders, argQuotaStable, argTestComplexity, toggle, docked;
            char argTagOpen, argTagClose;
            string argTagPrefix, msg;
            string[] args;
            StringBuilder sb;
            List<IMyTerminalBlock> blocks;

            sb = new StringBuilder();

            // initialize a few things on the very first run
            if (numCalls == 0)
            {
                CHARS_WHITESPACE = new char[3];
                CHARS_WHITESPACE[0] = ' ';
                CHARS_WHITESPACE[1] = '\t';
                CHARS_WHITESPACE[2] = '\u00AD';
                CHARS_COLON = new char[1];
                CHARS_COLON[0] = ':';
                CHARS_NEWLINE = new char[1];
                CHARS_NEWLINE[0] = '\n';
                CHARS_SPACECOMMA = new char[3];
                CHARS_SPACECOMMA[0] = ' ';
                CHARS_SPACECOMMA[1] = ',';
                CHARS_SPACECOMMA[2] = '\n';
                ScreenFormatter.Init();

                if (true)
                {
                    // pre-define all vanilla item types, just so users aren't confused if they don't happen to have any when setting things up
                    // note that this list is NOT required for the script to function! TIM will pick up any new types it sees as it goes
                    // so it should still handle any new items added by mods or future updates without having to add them to this list
                    RegisterTypeSubLabel("AmmoMagazine", "Missile200mm");
                    RegisterTypeSubLabel("AmmoMagazine", "NATO_25x184mm");
                    RegisterTypeSubLabel("AmmoMagazine", "NATO_5p56x45mm");
                    RegisterTypeSubLabel("Component", "BulletproofGlass");
                    RegisterTypeSubLabel("Component", "Computer");
                    RegisterTypeSubLabel("Component", "Construction");
                    RegisterTypeSubLabel("Component", "Detector");
                    RegisterTypeSubLabel("Component", "Display");
                    RegisterTypeSubLabel("Component", "Explosives");
                    RegisterTypeSubLabel("Component", "Girder");
                    RegisterTypeSubLabel("Component", "GravityGenerator", "GravityGen");
                    RegisterTypeSubLabel("Component", "InteriorPlate");
                    RegisterTypeSubLabel("Component", "LargeTube");
                    RegisterTypeSubLabel("Component", "Medical");
                    RegisterTypeSubLabel("Component", "MetalGrid");
                    RegisterTypeSubLabel("Component", "Motor");
                    RegisterTypeSubLabel("Component", "PowerCell");
                    RegisterTypeSubLabel("Component", "RadioCommunication", "RadioComm");
                    RegisterTypeSubLabel("Component", "Reactor");
                    RegisterTypeSubLabel("Component", "SmallTube");
                    RegisterTypeSubLabel("Component", "SolarCell");
                    RegisterTypeSubLabel("Component", "SteelPlate");
                    RegisterTypeSubLabel("Component", "Superconductor");
                    RegisterTypeSubLabel("Component", "Thrust");
                    RegisterTypeSubLabel("GasContainerObject", "HydrogenBottle");
                    RegisterTypeSubLabel("Ingot", "Cobalt");
                    RegisterTypeSubLabel("Ingot", "Gold");
                    RegisterTypeSubLabel("Ingot", "Iron");
                    RegisterTypeSubLabel("Ingot", "Magnesium");
                    RegisterTypeSubLabel("Ingot", "Nickel");
                    RegisterTypeSubLabel("Ingot", "Platinum");
                    //	RegisterTypeSubLabel("Ingot", "Scrap"); // Old Scrap Metal is no longer obtainable as far as I know
                    RegisterTypeSubLabel("Ingot", "Silicon");
                    RegisterTypeSubLabel("Ingot", "Silver");
                    RegisterTypeSubLabel("Ingot", "Stone");
                    RegisterTypeSubLabel("Ingot", "Uranium");
                    RegisterTypeSubLabel("Ore", "Cobalt");
                    RegisterTypeSubLabel("Ore", "Gold");
                    RegisterTypeSubLabel("Ore", "Ice");
                    RegisterTypeSubLabel("Ore", "Iron");
                    RegisterTypeSubLabel("Ore", "Magnesium");
                    RegisterTypeSubLabel("Ore", "Nickel");
                    //	RegisterTypeSubLabel("Ore", "Organic"); // Organic material is not obtainable as far as I know
                    RegisterTypeSubLabel("Ore", "Platinum");
                    RegisterTypeSubLabel("Ore", "Scrap");
                    RegisterTypeSubLabel("Ore", "Silicon");
                    RegisterTypeSubLabel("Ore", "Silver");
                    RegisterTypeSubLabel("Ore", "Stone");
                    RegisterTypeSubLabel("Ore", "Uranium");
                    RegisterTypeSubLabel("OxygenContainerObject", "OxygenBottle");
                    RegisterTypeSubLabel("PhysicalGunObject", "AngleGrinderItem");
                    RegisterTypeSubLabel("PhysicalGunObject", "AngleGrinder2Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "AngleGrinder3Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "AngleGrinder4Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "AutomaticRifleItem", "AutomaticRifle");
                    RegisterTypeSubLabel("PhysicalGunObject", "HandDrillItem");
                    RegisterTypeSubLabel("PhysicalGunObject", "HandDrill2Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "HandDrill3Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "HandDrill4Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "PreciseAutomaticRifleItem", "PreciseAutomaticRifle");
                    RegisterTypeSubLabel("PhysicalGunObject", "RapidFireAutomaticRifleItem", "RapidFireAutomaticRifle");
                    RegisterTypeSubLabel("PhysicalGunObject", "UltimateAutomaticRifleItem", "UltimateAutomaticRifle");
                    RegisterTypeSubLabel("PhysicalGunObject", "WelderItem");
                    RegisterTypeSubLabel("PhysicalGunObject", "Welder2Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "Welder3Item");
                    RegisterTypeSubLabel("PhysicalGunObject", "Welder4Item");
                }
            }

            // output terminal info
            numCalls += 1;
            Echo("Taleden's Inventory Manager");
            Echo("v" + VERS_MAJ + "." + VERS_MIN + "." + VERS_REV + " (" + VERS_UPD + ")");
            Echo("Last Run: #" + numCalls + " at " + dtStart.ToString("h:mm:ss tt"));

            // reset status and debugging data every cycle
            debugText.Clear();
            debugLogic.Clear();
            numXfers = 0;
            numRefs = 0;
            numAsms = 0;

            // parse arguments
            toggle = true;
            argRewriteTags = REWRITE_TAGS;
            argTagOpen = TAG_OPEN;
            argTagClose = TAG_CLOSE;
            argTagPrefix = TAG_PREFIX;
            argCycle = CYCLE_LENGTH;
            argScanCollectors = SCAN_COLLECTORS;
            argScanDrills = SCAN_DRILLS;
            argScanGrinders = SCAN_GRINDERS;
            argScanWelders = SCAN_WELDERS;
            argQuotaStable = QUOTA_STABLE;
            argTestComplexity = false;
            args = argument.Split(CHARS_WHITESPACE, StringSplitOptions.RemoveEmptyEntries);
            for (i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("rewrite", StringComparison.OrdinalIgnoreCase))
                {
                    argRewriteTags = true;
                    debugText.Add("Tag rewriting enabled");
                }
                else if (args[i].Equals("norewrite", StringComparison.OrdinalIgnoreCase))
                {
                    argRewriteTags = false;
                    debugText.Add("Tag rewriting disabled");
                }
                else if (args[i].StartsWith("tags=", StringComparison.OrdinalIgnoreCase))
                {
                    if (args[i].Length != 7)
                    {
                        Echo("Invalid 'tags=' delimiters \"" + args[i].Substring(5) + "\": must be exactly two characters");
                        toggle = false;
                    }
                    else if (args[i][5] == ' ' || args[i][6] == ' ')
                    {
                        Echo("Invalid 'tags=' delimiters \"" + args[i].Substring(5) + "\": cannot be spaces");
                        toggle = false;
                    }
                    else if (char.ToUpper(args[i][5]) == char.ToUpper(args[i][6]))
                    {
                        Echo("Invalid 'tags=' delimiters \"" + args[i].Substring(5) + "\": characters must be different");
                        toggle = false;
                    }
                    else
                    {
                        argTagOpen = char.ToUpper(args[i][5]);
                        argTagClose = char.ToUpper(args[i][6]);
                        debugText.Add("Tags are delimited by \"" + argTagOpen + "\" and \"" + argTagClose + "\"");
                    }
                }
                else if (args[i].StartsWith("prefix=", StringComparison.OrdinalIgnoreCase))
                {
                    argTagPrefix = args[i].Substring(7).Trim().ToUpper();
                    if (argTagPrefix == "")
                    {
                        debugText.Add("Tag prefix disabled");
                    }
                    else
                    {
                        debugText.Add("Tag prefix is \"" + argTagPrefix + "\"");
                    }
                }
                else if (args[i].StartsWith("cycle=", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(args[i].Substring(6), out argCycle) == false || argCycle < 1)
                    {
                        Echo("Invalid 'cycle=' length \"" + args[i].Substring(6) + "\": must be a positive integer");
                        toggle = false;
                    }
                    else
                    {
                        argCycle = Math.Min(Math.Max(argCycle, 1), MAX_CYCLE_STEPS);
                        if (argCycle < 2)
                        {
                            debugText.Add("Function cycling disabled");
                        }
                        else
                        {
                            debugText.Add("Cycle length is " + argCycle);
                        }
                    }
                }
                else if (args[i].StartsWith("scan=", StringComparison.OrdinalIgnoreCase))
                {
                    args[i] = args[i].Substring(5);
                    if (args[i].Equals("collectors", StringComparison.OrdinalIgnoreCase))
                    {
                        argScanCollectors = true;
                        debugText.Add("Enabled scanning of Collectors");
                    }
                    else if (args[i].Equals("drills", StringComparison.OrdinalIgnoreCase))
                    {
                        argScanDrills = true;
                        debugText.Add("Enabled scanning of Drills");
                    }
                    else if (args[i].Equals("grinders", StringComparison.OrdinalIgnoreCase))
                    {
                        argScanGrinders = true;
                        debugText.Add("Enabled scanning of Grinders");
                    }
                    else if (args[i].Equals("welders", StringComparison.OrdinalIgnoreCase))
                    {
                        argScanWelders = true;
                        debugText.Add("Enabled scanning of Welders");
                    }
                    else
                    {
                        Echo("Invalid 'scan=' block type '" + args[i] + "': must be 'collectors', 'drills', 'grinders' or 'welders'");
                        toggle = false;
                    }
                }
                else if (args[i].StartsWith("quota=", StringComparison.OrdinalIgnoreCase))
                {
                    args[i] = args[i].Substring(6);
                    if (args[i].Equals("literal", StringComparison.OrdinalIgnoreCase))
                    {
                        argQuotaStable = false;
                        debugText.Add("Disabled stable dynamic quotas");
                    }
                    else if (args[i].Equals("stable", StringComparison.OrdinalIgnoreCase))
                    {
                        argQuotaStable = true;
                        debugText.Add("Enabled stable dynamic quotas");
                    }
                    else
                    {
                        Echo("Invalid 'quota=' mode '" + args[i] + "': must be 'literal' or 'stable'");
                        toggle = false;
                    }
                }
                else if (args[i].StartsWith("debug=", StringComparison.OrdinalIgnoreCase))
                {
                    args[i] = args[i].Substring(6);
                    if (args[i].Length >= 1 & "sorting".StartsWith(args[i], StringComparison.OrdinalIgnoreCase))
                    {
                        debugLogic.Add("sorting");
                    }
                    else if (args[i].Length >= 1 & "refineries".StartsWith(args[i], StringComparison.OrdinalIgnoreCase))
                    {
                        debugLogic.Add("refineries");
                    }
                    else if (args[i].Length >= 1 & "assemblers".StartsWith(args[i], StringComparison.OrdinalIgnoreCase))
                    {
                        debugLogic.Add("assemblers");
                    }
                    else
                    {
                        Echo("Invalid 'debug=' type '" + args[i] + "': must be 'sorting', 'refineries' or 'assemblers'");
                        toggle = false;
                    }
                }
                else if (args[i].Equals("complexity", StringComparison.OrdinalIgnoreCase))
                {
                    argTestComplexity = true;
                }
                else
                {
                    Echo("Unrecognized argument: " + args[i]);
                    toggle = false;
                }
            }
            if (toggle == false)
                return;

            // apply changed arguments
            toggle = (tagOpen != argTagOpen) | (tagClose != argTagClose) | (tagPrefix != argTagPrefix);
            if ((toggle | (rewriteTags != argRewriteTags) | (cycleLength != argCycle)) && (cycleStep > 0))
            {
                cycleStep = 0;
                msg = "Options changed; cycle step reset.";
                Echo(msg);
                debugText.Add(msg);
            }
            rewriteTags = argRewriteTags;
            tagOpen = argTagOpen;
            tagClose = argTagClose;
            tagPrefix = argTagPrefix;
            cycleLength = argCycle;
            if (tagRegex == null | toggle)
            {
                msg = "\\" + tagOpen;
                if (tagPrefix != "")
                {
                    msg += " *" + System.Text.RegularExpressions.Regex.Escape(tagPrefix) + "(|[ ,]+[^\\" + tagClose + "]*)";
                }
                else
                {
                    msg += "([^\\" + tagClose + "]*)";
                }
                msg += "\\" + tagClose;
                tagRegex = new System.Text.RegularExpressions.Regex(msg, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            }

            // scan connectors before PGs! if another TIM is on a grid that is *not* correctly docked, both still need to run
            if (cycleStep == 0 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Scanning grid connectors ...";
                    Echo(msg);
                    debugText.Add(msg);
                }

                // reset everything that we'll check during this step
                gridDocked = new Dictionary<IMyCubeGrid, bool>();

                ScanGrids();
            }

            // search for other TIMs
            blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, block => { return (block == Me) | (tagRegex.IsMatch(block.CustomData) & gridDocked.TryGetValue(block.CubeGrid, out docked) & docked == true); });
            i = blocks.IndexOf(Me);
            j = blocks.FindIndex(block => { return block.IsFunctional & block.IsWorking; });
            if (blocks.Count > 1)
            {
                msg = tagOpen + tagPrefix + " #" + (i + 1) + tagClose;
            }
            else
            {
                msg = tagOpen + tagPrefix + tagClose;
            }
            if (tagRegex.IsMatch(Me.CustomData))
            {
                Me.CustomData = tagRegex.Replace(Me.CustomData, msg, 1);
            }
            else
            {
                Me.CustomData = Me.CustomData + " " + msg;
            }
            if (i != j)
            {
                Echo("TIM #" + (j + 1) + " is on duty. Standing by.");
                if (("" + (blocks[j] as IMyProgrammableBlock).TerminalRunArgument).Trim() != ("" + Me.TerminalRunArgument).Trim())
                    Echo("WARNING: Script arguments do not match TIM #" + (j + 1) + ".");
                return;
            }

            // TODO: API testing
            /* *
                blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks);
                HashSet<string> set = new HashSet<string>();
                for (i = 0;  i < blocks.Count;  i++) {
                    string def = blocks[i].BlockDefinition.TypeIdString + "/" + blocks[i].BlockDefinition.SubtypeId;
                    if (set.Contains(def) == false) {
                        debugText.Add(def);
                        debugText.Add(" called '"+blocks[i].DefinitionDisplayNameText+"', has "+blocks[i].GetInventoryCount()+" invens");
                        debugText.Add(" inv[0] = "+blocks[i].GetInventory(0));
                        debugText.Add(" inv[0].Owner = "+blocks[i].GetInventory(0).Owner);
                        set.Add(def);
                    }
                }
            /* */

            if (cycleStep == 1 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Scanning inventories ...";
                    Echo(msg);
                    debugText.Add(msg);
                }

                // reset everything that we'll check during this step
                types.ForEach((string itype) => {
                    typeAmount[itype] = 0;
                    typeHidden[itype] = 0;
                    typeSubs[itype].ForEach((string isub) => {
                        typeSubAmount[itype][isub] = 0L;
                        typeSubHidden[itype][isub] = 0L;
                        typeSubAvail[itype][isub] = 0L;
                        typeSubLocked[itype][isub] = 0L;
                        typeSubInvenTotal[itype][isub].Clear();
                        typeSubInvenSlot[itype][isub].Clear();
                    });
                });
                blockTag.Clear();
                invenLocked.Clear();
                invenHidden.Clear();

                // scan inventories
                ScanBlocks<IMyAssembler>();
                ScanBlocks<IMyCargoContainer>();
                if (argScanCollectors)
                    ScanBlocks<IMyCollector>();
                ScanBlocks<IMyOxygenGenerator>();
                ScanBlocks<IMyOxygenTank>();
                ScanBlocks<IMyReactor>();
                ScanBlocks<IMyRefinery>();
                ScanBlocks<IMyShipConnector>();
                //	ScanBlocks<IMyShipController>(); // TODO whenever Keen fixes inven.Owner for these inventories
                if (argScanDrills)
                    ScanBlocks<IMyShipDrill>();
                if (argScanGrinders)
                    ScanBlocks<IMyShipGrinder>();
                if (argScanWelders)
                    ScanBlocks<IMyShipWelder>();
                ScanBlocks<IMyTextPanel>();
                ScanBlocks<IMyUserControllableGun>();

                // if we found any new item type/subtypes, re-sort the lists
                if (foundNewItem)
                {
                    foundNewItem = false;
                    types.Sort();
                    types.ForEach((string itype) => { typeSubs[itype].Sort(); });
                    subs.Sort();
                    subs.ForEach((string isub) => { subTypes[isub].Sort(); });
                }
            }

            if (cycleStep == 2 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Scanning tags ...";
                    Echo(msg);
                    debugText.Add(msg);
                }

                // reset everything that we'll check during this step
                types.ForEach((string itype) => {
                    itypePanels[itype].Clear();
                    qtypePanel[itype] = null;
                    qtypeErrors[itype].Clear();
                    typeSubs[itype].ForEach((string isub) => {
                        typeSubQuota[itype][isub] = 0L;
                        typeSubProducers[itype][isub].Clear();
                    });
                });
                priTypeSubInvenRequest.Clear();
                statusPanels.Clear();
                debugPanels.Clear();
                refineryOre.Clear();
                panelSpan.Clear();

                // parse tags
                ParseBlockTags();
            }

            if (cycleStep == 3 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Adjusting tallies ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                AdjustAmounts();
            }

            if (cycleStep == 4 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Scanning quotas ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                ParseQuotaPanels(argQuotaStable);
            }

            if (cycleStep == 5 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Updating quota panels ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                UpdateQuotaPanels();
            }

            if (cycleStep == 6 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Processing limited item requests ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                AllocateItems(true); // limited requests
            }

            if (cycleStep == 7 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Managing refinery assignments ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                ManageRefineries();
            }

            if (cycleStep == 8 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Processing remaining item requests ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                AllocateItems(false); // unlimited requests
            }

            if (cycleStep == 9 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Managing assemblers ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                ManageAssemblers();
            }

            if (cycleStep == 10 * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    msg = "Updating inventory panels ...";
                    Echo(msg);
                    debugText.Add(msg);
                }
                UpdateInventoryPanels();
            }

            // update script status and debug panels on every cycle step
            cycleStep++;
            runtime = (int)((DateTime.Now - dtStart).TotalMilliseconds + 0.5);
            if (cycleLength > 1)
            {
                msg = "Cycle " + cycleStep + " of " + cycleLength + " completed in " + runtime + " ms";
            }
            else
            {
                msg = "Completed in " + runtime + " ms";
            }
            Echo(msg);
            debugText.Add(msg);
            i = (int)(numCalls % statsLog.Length);
            statsLog[i].num = numCalls;
            statsLog[i].step = cycleStep;
            statsLog[i].cycle = cycleLength;
            statsLog[i].time = runtime;
            statsLog[i].xfers = numXfers;
            statsLog[i].refs = numRefs;
            statsLog[i].asms = numAsms;
            UpdateStatusPanels();
            if (cycleStep >= cycleLength)
                cycleStep = 0;

            // if we can spare the cycles, render the filler
            if (panelFiller == "" & numCalls > cycleLength)
            {
                panelFiller = "This used to be a fun easter egg, but it took up too much space in the script code.\nPlease ask Keen to raise the 100kb script code size limit!\n";
            }

            // yes, this is an infinite loop; the idea is to see how many iterations
            // remain before triggering the exception
            if (argTestComplexity)
            {
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks);
                Echo("Complexity testing; output to " + blocks[0].CustomName);
                for (i = 0; true; i++)
                    (blocks[0] as IMyTextPanel).WritePublicText(i.ToString(), false);
            }
        } // Main()


        /*
        * SCREENFORMATTER
        */


        public class ScreenFormatter
        {
            private static Dictionary<char, byte> charWidth = new Dictionary<char, byte>();
            private static Dictionary<string, int> textWidth = new Dictionary<string, int>();
            private static byte WIDTH_SPACE;
            private static byte WIDTH_SHYPH;
            //	private static byte WIDTH_UNDEFINED;


            public static int GetWidth(string text, bool memoize = false)
            {
                int width;
                if (!textWidth.TryGetValue(text, out width))
                {
                    /*
                    Unrolling the loops like this doesn't actually make them *faster*
                    in any meaningful way, but it does use fewer loop iterations, which
                    have a silly arbitrary limit in SpaceEngineers; doing it this way
                    is ~50% less "complex" than a simple for(;;i++) loop.
                    */
                    int i = text.Length;
                    byte[] w = new byte[8];
                    if (i % 8 > 0)
                    {
                        charWidth.TryGetValue(text[--i], out w[0]);
                        if (i % 8 > 0)
                        {
                            charWidth.TryGetValue(text[--i], out w[1]);
                            if (i % 8 > 0)
                            {
                                charWidth.TryGetValue(text[--i], out w[2]);
                                if (i % 8 > 0)
                                {
                                    charWidth.TryGetValue(text[--i], out w[3]);
                                    if (i % 8 > 0)
                                    {
                                        charWidth.TryGetValue(text[--i], out w[4]);
                                        if (i % 8 > 0)
                                        {
                                            charWidth.TryGetValue(text[--i], out w[5]);
                                            if (i % 8 > 0)
                                            {
                                                charWidth.TryGetValue(text[--i], out w[6]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    width += w[0] + w[1] + w[2] + w[3] + w[4] + w[5] + w[6];
                    while (i > 0)
                    {
                        charWidth.TryGetValue(text[i - 1], out w[0]);
                        charWidth.TryGetValue(text[i - 2], out w[1]);
                        charWidth.TryGetValue(text[i - 3], out w[2]);
                        charWidth.TryGetValue(text[i - 4], out w[3]);
                        charWidth.TryGetValue(text[i - 5], out w[4]);
                        charWidth.TryGetValue(text[i - 6], out w[5]);
                        charWidth.TryGetValue(text[i - 7], out w[6]);
                        charWidth.TryGetValue(text[i - 8], out w[7]);
                        width += w[0] + w[1] + w[2] + w[3] + w[4] + w[5] + w[6] + w[7];
                        i -= 8;
                    }
                    if (memoize & text.Length > 3)
                        textWidth[text] = width;
                }
                return width;
            } // GetWidth()


            public static string Format(string text, int width, out int unused, int align = -1, bool memoize = false)
            {
                int spaces, bars;

                /*
                The character '\u00AD' is meant to be a "soft hyphen" in the UTF16
                character set used by Space Engineers, but since text panels don't
                actually wrap long lines, it only ever displays as a blank space.
                But it's slightly wider than the regular space character ' ',
                which makes it very useful for column alignment.
                */
                unused = width - GetWidth(text, memoize);
                if (unused <= WIDTH_SPACE / 2)
                    return text;
                spaces = unused / WIDTH_SPACE;
                bars = 0;
                unused -= spaces * WIDTH_SPACE;
                if (2 * unused <= WIDTH_SPACE + (spaces * (WIDTH_SHYPH - WIDTH_SPACE)))
                {
                    bars = Math.Min(spaces, (int)((float)unused / (WIDTH_SHYPH - WIDTH_SPACE) + 0.4999f));
                    spaces -= bars;
                    unused -= bars * (WIDTH_SHYPH - WIDTH_SPACE);
                }
                else if (unused > WIDTH_SPACE / 2)
                {
                    spaces++;
                    unused -= WIDTH_SPACE;
                }
                if (align > 0)
                {
                    text = new String(' ', spaces) + new String('\u00AD', bars) + text;
                }
                else if (align < 0)
                {
                    text = text + new String('\u00AD', bars) + new String(' ', spaces);
                }
                else if ((spaces % 2) > 0 & (bars % 2) == 0)
                {
                    text = new String(' ', spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars / 2) + new String(' ', spaces - (spaces / 2));
                }
                else
                {
                    text = new String(' ', spaces - (spaces / 2)) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars - (bars / 2)) + new String(' ', spaces / 2);
                }
                return text;
            } // Format()


            public static string Format(double value, int width, out int unused)
            {
                int spaces, bars;

                value = Math.Min(Math.Max(value, 0.0f), 1.0f);
                spaces = width / WIDTH_SPACE;
                bars = (int)(spaces * value + 0.5f);
                unused = width - (spaces * WIDTH_SPACE);
                return new String('I', bars) + new String(' ', spaces - bars);
            } // Format()


            private static void DefineCharWidths(byte width, string text)
            {
                // more silly loop-unrolling, as in GetWidth()
                int i = text.Length;
                if (i % 8 > 0)
                {
                    charWidth[text[--i]] = width;
                    if (i % 8 > 0)
                    {
                        charWidth[text[--i]] = width;
                        if (i % 8 > 0)
                        {
                            charWidth[text[--i]] = width;
                            if (i % 8 > 0)
                            {
                                charWidth[text[--i]] = width;
                                if (i % 8 > 0)
                                {
                                    charWidth[text[--i]] = width;
                                    if (i % 8 > 0)
                                    {
                                        charWidth[text[--i]] = width;
                                        if (i % 8 > 0)
                                        {
                                            charWidth[text[--i]] = width;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                while (i > 0)
                {
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                    charWidth[text[--i]] = width;
                }
            } // DefineCharWidths()


            public static void Init()
            {
                DefineCharWidths(0, "\u2028\u2029\u202F");
                DefineCharWidths(7, "'|\u00A6\u02C9\u2018\u2019\u201A");
                DefineCharWidths(8, "\u0458");
                DefineCharWidths(9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
                DefineCharWidths(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");
                DefineCharWidths(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");
                DefineCharWidths(12, "*\u00B2\u00B3\u00B9");
                DefineCharWidths(13, "\\\u00B0\u201C\u201D\u201E");
                DefineCharWidths(14, "\u0491");
                DefineCharWidths(15, "/\u0133\u0442\u044D\u0454");
                DefineCharWidths(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
                DefineCharWidths(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
                DefineCharWidths(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F");
                DefineCharWidths(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
                DefineCharWidths(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC");
                DefineCharWidths(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
                DefineCharWidths(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
                DefineCharWidths(23, "\u0459");
                DefineCharWidths(24, "\u044E");
                DefineCharWidths(25, "%\u0132\u042B");
                DefineCharWidths(26, "@\u00A9\u00AE\u043C\u0448\u045A");
                DefineCharWidths(27, "M\u041C\u0428");
                DefineCharWidths(28, "mw\u00BC\u0175\u042E\u0449");
                DefineCharWidths(29, "\u00BE\u00E6\u0153\u0409");
                DefineCharWidths(30, "\u00BD\u0429");
                DefineCharWidths(31, "\u2122");
                DefineCharWidths(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
                WIDTH_SPACE = charWidth[' '];
                WIDTH_SHYPH = charWidth['\u00AD'];
                //		WIDTH_UNDEFINED = 22;
            } // Init()


            private int numCols;
            private int numRows;
            private int padding;
            private List<string>[] colRowText;
            private List<int>[] colRowWidth;
            private int[] colAlign;
            private int[] colFill;
            private bool[] colBar;
            private int[] colWidth;


            public ScreenFormatter(int numCols, int padding = 1)
            {
                this.numCols = numCols;
                this.numRows = 0;
                this.padding = padding;
                this.colRowText = new List<string>[numCols];
                this.colRowWidth = new List<int>[numCols];
                this.colAlign = new int[numCols];
                this.colFill = new int[numCols];
                this.colBar = new bool[numCols];
                this.colWidth = new int[numCols];
                for (int c = 0; c < numCols; c++)
                {
                    this.colRowText[c] = new List<string>();
                    this.colRowWidth[c] = new List<int>();
                    this.colAlign[c] = -1;
                    this.colFill[c] = 0;
                    this.colBar[c] = false;
                    this.colWidth[c] = 0;
                }
            } // ScreenFormatter()


            public void Append(int col, string text, bool memoize = false)
            {
                int width = 0;
                this.colRowText[col].Add(text);
                if (this.colBar[col] == false)
                {
                    width = GetWidth(text, memoize);
                    this.colWidth[col] = Math.Max(this.colWidth[col], width);
                }
                this.colRowWidth[col].Add(width);
                this.numRows = Math.Max(this.numRows, this.colRowText[col].Count);
            } // Append()


            public int GetNumRows()
            {
                return this.numRows;
            } // GetNumRows()


            public int GetMinWidth()
            {
                int width = this.padding * WIDTH_SPACE;
                for (int c = 0; c < this.numCols; c++)
                {
                    width += this.padding * WIDTH_SPACE + this.colWidth[c];
                }
                return width;
            } // GetMinWidth()


            public void SetAlign(int col, int align)
            {
                this.colAlign[col] = align;
            } // SetAlign()


            public void SetFill(int col, int fill = 1)
            {
                this.colFill[col] = fill;
            } // SetFill()


            public void SetBar(int col, bool bar = true)
            {
                this.colBar[col] = bar;
            } // SetBar()


            public void SetWidth(int col, int width)
            {
                this.colWidth[col] = width;
            } // SetWidth()


            public string[][] ToSpan(int width = 0, int span = 1)
            {
                int c, r, s, i, j, textwidth, unused, remaining;
                int[] colWidth;
                byte w;
                double value;
                string text;
                StringBuilder sb;
                string[][] spanLines;

                // clone the user-defined widths and tally fill columns
                colWidth = (int[])this.colWidth.Clone();
                unused = width * span - this.padding * WIDTH_SPACE;
                remaining = 0;
                for (c = 0; c < this.numCols; c++)
                {
                    unused -= this.padding * WIDTH_SPACE;
                    if (this.colFill[c] == 0)
                        unused -= colWidth[c];
                    remaining += this.colFill[c];
                }

                // distribute remaining width to fill columns
                for (c = 0; c < this.numCols & remaining > 0; c++)
                {
                    if (this.colFill[c] > 0)
                    {
                        colWidth[c] = Math.Max(colWidth[c], this.colFill[c] * unused / remaining);
                        unused -= colWidth[c];
                        remaining -= this.colFill[c];
                    }
                }

                // initialize output arrays
                spanLines = new string[span][];
                for (s = 0; s < span; s++)
                    spanLines[s] = new string[this.numRows];
                span--; // change "span" to an inclusive limit so we can use "s < span" to see if we have one left

                // render all rows and columns
                i = 0;
                sb = new StringBuilder();
                for (r = 0; r < this.numRows; r++)
                {
                    sb.Clear();
                    s = 0;
                    remaining = width;
                    unused = 0;
                    for (c = 0; c < this.numCols; c++)
                    {
                        unused += this.padding * WIDTH_SPACE;
                        if (r >= this.colRowText[c].Count || colRowText[c][r] == "")
                        {
                            unused += colWidth[c];
                        }
                        else
                        {
                            // render the bar, or fetch the cell text
                            text = this.colRowText[c][r];
                            charWidth.TryGetValue(text[0], out w);
                            textwidth = this.colRowWidth[c][r];
                            if (this.colBar[c] == true)
                            {
                                value = 0.0;
                                if (double.TryParse(text, out value))
                                    value = Math.Min(Math.Max(value, 0.0), 1.0);
                                i = (int)((colWidth[c] / WIDTH_SPACE) * value + 0.5);
                                w = WIDTH_SPACE;
                                textwidth = i * WIDTH_SPACE;
                            }

                            // if the column is not left-aligned, calculate left spacing
                            if (this.colAlign[c] > 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (this.colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) / 2;
                            }

                            // while the left spacing leaves no room for text, adjust it
                            while (s < span & unused > remaining - w)
                            {
                                sb.Append(' ');
                                spanLines[s][r] = sb.ToString();
                                sb.Clear();
                                s++;
                                unused -= remaining;
                                remaining = width;
                            }

                            // add left spacing
                            remaining -= unused;
                            sb.Append(Format("", unused, out unused));
                            remaining += unused;

                            // if the column is not right-aligned, calculate right spacing
                            if (this.colAlign[c] < 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (this.colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) - ((colWidth[c] - textwidth) / 2);
                            }

                            // while the bar or text runs to the next span, split it
                            if (this.colBar[c] == true)
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    j = remaining / WIDTH_SPACE;
                                    remaining -= j * WIDTH_SPACE;
                                    textwidth -= j * WIDTH_SPACE;
                                    sb.Append(new String('I', j));
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    i -= j;
                                }
                                text = new String('I', i);
                            }
                            else
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    i = 0;
                                    while (remaining >= w)
                                    {
                                        remaining -= w;
                                        textwidth -= w;
                                        charWidth.TryGetValue(text[++i], out w);
                                    }
                                    sb.Append(text, 0, i);
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    text = text.Substring(i);
                                }
                            }

                            // add cell text
                            remaining -= textwidth;
                            sb.Append(text);
                        }
                    }
                    spanLines[s][r] = sb.ToString();
                }

                return spanLines;
            } // GetLines()


            public string ToString(int width = 0)
            {
                return String.Join("\n", this.ToSpan(width, 1)[0]);
            } // ToString()


        } // ScreenFormatter
        #endregion

    }
}
