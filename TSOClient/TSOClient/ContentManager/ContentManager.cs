﻿/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Linq;
using System.Xml;
using SimsLib.FAR3;
using DNA;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR1;
using TSOClient.LUI;
using LogThis;

namespace TSOClient
{
    public delegate void OnLoadingUpdatedDelegate(string LoadingText);

    public class ContentManager
    {
        private const int m_CACHESIZE = 104857600; //100 megabytes.
        private static int m_CurrentCacheSize = 0;

        private static Dictionary<ulong, string> m_Resources;
        private static Dictionary<ulong, byte[]> m_LoadedResources;
        private static bool initComplete = false;
        private static List<Floor> m_Floors;
        private static List<Wall> m_Walls;
        private static Wall m_DefaultWall;
        private static Floor m_DefaultFloor;

        private static ManualResetEvent m_ResetEvent = new ManualResetEvent(false);
        public static event OnLoadingUpdatedDelegate OnLoadingUpdatedEvent;

        public static Wall DefaultWall { get { return m_DefaultWall; } }
        public static Floor DefaultFloor { get { return m_DefaultFloor; } }
        public static List<Floor> Floors { get { return m_Floors; } }
        public static List<Wall> Walls { get { return m_Walls; } }
        
        static ContentManager()
        {
            m_Resources = new Dictionary<ulong, string>();
            m_LoadedResources = new Dictionary<ulong, byte[]>();

            XmlDataDocument AccessoryTable = new XmlDataDocument();
            AccessoryTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\accessorytable.xml");

            XmlNodeList NodeList = AccessoryTable.GetElementsByTagName("DefineAssetString");

            foreach(XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                //TODO: Figure out when to use avatardata2 and avatardata3...
                string FileName = GlobalSettings.Default.StartupPath + "avatardata\\accessories\\accessories.dat";

                //Some duplicates are known to exist...
                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument AnimTable = new XmlDataDocument(); 
            AnimTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\animtable.xml");

            NodeList = AnimTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                //TODO: Figure out when to use avatardata2 and avatardata3...
                string FileName = GlobalSettings.Default.StartupPath + "avatardata\\animations\\animations.dat";

                //Some duplicates are known to exist...
                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument UIGraphicsTable = new XmlDataDocument();
            UIGraphicsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\uigraphics.xml");

            NodeList = UIGraphicsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);

                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }
                else
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;

                //Some duplicates are known to exist...
                if(!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument CollectionsTable = new XmlDataDocument();
            CollectionsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\collections.xml");

            NodeList = CollectionsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument PurchasablesTable = new XmlDataDocument();
            PurchasablesTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\purchasables.xml");

            NodeList = PurchasablesTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument OutfitsTable = new XmlDataDocument();
            OutfitsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\alloutfits.xml");

            NodeList = PurchasablesTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                //Some duplicates are known to exist...
                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            initComplete = true;
        }

        public static byte[] GetResourceFromLongID(ulong ID)
        {
            while (!initComplete) ;
            if (m_Resources.ContainsKey(ID))
            {
                //Resource hasn't already been loaded...
                if (!m_LoadedResources.ContainsKey(ID))
                {
                    string path = m_Resources[ID];

                    FAR3Archive Archive = new FAR3Archive(path);

                    byte[] Resource = Archive.GetItemByID(ID);
                    TryToStoreResource(ID, Resource);
                    return Resource;
                }
                else
                    return m_LoadedResources[ID];
            }
            
            return new byte[0];
        }

        public byte[] this[ulong FileID]
        {
            get
            {
                if (m_Resources.ContainsKey(FileID))
                {
                    //Resource hasn't already been loaded...
                    if (!m_LoadedResources.ContainsKey(FileID))
                    {
                        string path = m_Resources[FileID].Replace("./", "");
                        if (!File.Exists(path))
                        {
                            string[] pathSections = path.Split(new char[] { '/' });
                            string directoryName = pathSections[pathSections.Length - 2];
                            string archivePath = GlobalSettings.Default.StartupPath + path.Remove(path.LastIndexOf('/') + 1) + directoryName + ".dat";

                            FAR3Archive archive = new FAR3Archive(archivePath);
                            TryToStoreResource(FileID, archive[pathSections[pathSections.Length - 1]]);
                            return archive[pathSections[pathSections.Length - 1]];
                        }
                        else
                        {
                            byte[] Resource = File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);

                            TryToStoreResource(FileID, Resource);
                            return Resource;
                        }
                    }
                    else
                        return m_LoadedResources[FileID];
                }
                return new byte[0];
            }
        }

        /// <summary>
        /// Tries to store a resource in the internal cache.
        /// </summary>
        /// <param name="ID">The ID of the resource to store.</param>
        /// <param name="Resource">The resource to store.</param>
        private static void TryToStoreResource(ulong ID, byte[] Resource)
        {
            lock (m_LoadedResources)
            {
                if (m_CurrentCacheSize < m_CACHESIZE)
                {
                    m_LoadedResources.Add(ID, Resource);
                    m_CurrentCacheSize += Resource.Length;
                }
                else
                {
                    ulong LastKey = m_LoadedResources.Keys.Last();

                    m_CurrentCacheSize -= m_LoadedResources[LastKey].Length;
                    m_LoadedResources.Remove(LastKey);

                    m_LoadedResources.Add(ID, Resource);
                    m_CurrentCacheSize += Resource.Length;
                }
            }
        }

        private static void LoadInitialTextures()
        {
            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //These textures are needed for the logindialog, so preload them.
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.dialog_backgroundtemplate);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.buttontiledialog);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.dialog_progressbarback);

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //Textures for the personselection screen.
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_background);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_exitbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_simcreatebtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_cityhouseiconalpha);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_arrowdownbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_arrowupbtn);

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //Textures for the CAS screen.
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_background);
            //GetResourceFromLongID(0x3dd00000001); //person_edit_backtoselectbtn.bmp
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_cancelbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_closebtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_femalebtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_malebtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skindarkbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinmediumbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowleft);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowright);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinlightbtn);

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cas_sas_creditsbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cas_sas_creditsindent);

            //Textures for the credits screen.
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_backbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_backbtnindent);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_background);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_exitbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_maxisbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_tsologo_english);

            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cityselector_sortbtn);
            GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cityselector_thumbnailbackground);

            myLoadingScreenEWH.Set();
        }

        private static EventWaitHandle myLoadingScreenEWH;

        /// <summary>
        /// Initializes loading of resources.
        /// </summary>
        /// <param name="ScreenMgr">A ScreenManager instance, used to access a GraphicsDevice.</param>
        public static void InitLoading()
        {
            myLoadingScreenEWH = new EventWaitHandle(false, EventResetMode.ManualReset, "Go_Away_Stupid_Loading_Screen_GO_U_HEARD_ME_DONT_MAKE_ME_GET_MY_STICK_OUT");
            //ThreadPool.QueueUserWorkItem(new WaitCallback(LoadContent));
            Thread T = new Thread(new ParameterizedThreadStart(LoadContent));
            T.Start();
        }

        /// <summary>
        /// Threading function that takes care of loading.
        /// </summary>
        private static void LoadContent(object ThreadObject)
        {
            //InitWalls(Manager.GraphicsDevice);
            //InitFloors(Manager.GraphicsDevice);
            LoadInitialTextures();
        }
    }
}