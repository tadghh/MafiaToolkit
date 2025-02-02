﻿using Mafia2Tool;
using ResourceTypes.Actors;
using ResourceTypes.SoundTable;
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static ResourceTypes.FrameNameTable.FrameNameTable;
using Gibbed.Squish;
using ResourceTypes.Navigation.Traffic;

namespace Core.IO
{
    public class FileRoadmapDE : FileRoadMap
    {
        public FileRoadmapDE(FileInfo info) : base(info)
        {
        }

        public override string GetExtensionUpper()
        {
            return "GAME";
        }

        protected override IRoadmap GetNewRoadmap()
        {
            return new RoadmapDe();
        }

        protected override IRoadmapFactory GetNewRoadmapFactory()
        {
            return new RoadmapFactoryDe();
        }
    }

    public class FileRoadmapClassic : FileRoadMap
    {
        public FileRoadmapClassic(FileInfo info) : base(info)
        {
        }

        public override string GetExtensionUpper()
        {
            return "GSD";
        }

        protected override IRoadmap GetNewRoadmap()
        {
            return new RoadmapCe();
        }

        protected override IRoadmapFactory GetNewRoadmapFactory()
        {
            return new RoadmapFactoryCe();
        }
    }

    public class FileRoadMap : FileBase
    {
        public FileRoadMap(FileInfo info) : base(info)
        {
        }

        public override string GetExtensionUpper()
        {
            return "XXXX";
        }

        public override bool Open()
        {
            // TODO: Make editor

            SaveFileDialog saveFile = new SaveFileDialog()
            {
                InitialDirectory = Path.GetDirectoryName(file.FullName),
                FileName = Path.GetFileNameWithoutExtension(file.FullName),
                Filter = "XML (*.xml)|*.xml"
            };

            if (saveFile.ShowDialog() == true)
            {
                ConvertToXML(GetNewRoadmap(), saveFile.FileName);
            }

            return true;
        }

        public override void Save()
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                InitialDirectory = Path.GetDirectoryName(file.FullName),
                FileName = Path.GetFileNameWithoutExtension(file.FullName),
                Filter = "XML (*.xml)|*.xml"
            };

            if (openFile.ShowDialog() == true)
            {
                IRoadmap NewRoadmap = ConvertFromXml(GetNewRoadmapFactory(), openFile.FileName);
                using (FileStream FStream = File.Open(file.FullName, FileMode.Open))
                {
                    NewRoadmap.Write(FStream);
                }
            }
        }

        public override bool CanContextMenuOpen()
        {
            return true;
        }

        public override string GetContextMenuOpenTitle()
        {
            return "Convert To (.xml)";
        }

        public override bool CanContextMenuSave()
        {
            return true;
        }

        public override string GetContextMenuSaveTitle()
        {
            return "Convert From (.xml)";
        }

        private IRoadmap ConvertFromXml(IRoadmapFactory Factory, string Filename)
        {
            RoadmapXmlSerializer Serializer = new RoadmapXmlSerializer();
            return Serializer.Deserialize(Factory, Filename);
        }

        private void ConvertToXML(IRoadmap Roadmap, string Filename)
        {
            using (FileStream FStream = File.Open(file.FullName, FileMode.Open))
            {
                Roadmap.Read(FStream);
            }

            RoadmapXmlSerializer Serializer = new RoadmapXmlSerializer();
            Serializer.Serialize(Roadmap, Filename);
        }

        protected virtual IRoadmap GetNewRoadmap()
        {
            return null;
        }

        protected virtual IRoadmapFactory GetNewRoadmapFactory()
        {
            return null;
        }
    }
}
