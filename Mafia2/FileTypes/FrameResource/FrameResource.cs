﻿using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mafia2
{
    public class FrameResource
    {

        FrameHeader header;
        int[] frameBlocks;
        object[] frameObjects;
        List<Object> entireFrame = new List<object>();
        Dictionary<int, FrameHeaderScene> frameScenes = new Dictionary<int, FrameHeaderScene>();
        Dictionary<int, FrameGeometry> frameGeometries = new Dictionary<int, FrameGeometry>();
        Dictionary<int, FrameMaterial> frameMaterials = new Dictionary<int, FrameMaterial>();
        Dictionary<int, FrameBlendInfo> frameBlendInfos = new Dictionary<int, FrameBlendInfo>();
        Dictionary<int, FrameSkeleton> frameSkeletons = new Dictionary<int, FrameSkeleton>();
        Dictionary<int, FrameSkeletonHierachy> frameSkeletonHierachies = new Dictionary<int, FrameSkeletonHierachy>();

        int[] objectTypes;

        public FrameHeader Header {
            get { return header; }
            set { header = value; }
        }
        public int[] FrameBlocks {
            get { return frameBlocks; }
            set { frameBlocks = value; }
        }
        public object[] FrameObjects {
            get { return frameObjects; }
            set { frameObjects = value; }
        }
        public List<Object> EntireFrame {
            get { return entireFrame; }
        }

        public FrameResource(string file)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                ReadFromFile(reader);
            }
        }

        /// <summary>
        /// Reads the file into the memory.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadFromFile(BinaryReader reader)
        {
            header = new FrameHeader();
            header.ReadFromFile(reader);

            objectTypes = new int[header.NumObjects];
            frameBlocks = new int[header.SceneFolders.Length + header.NumGeometries + header.NumMaterialResources + header.NumBlendInfos + header.NumSkeletons + header.NumSkelHierachies];
            frameObjects = new object[header.NumObjects];

            int j = 0;

            for (int i = 0; i != header.SceneFolders.Length; i++)
            {
                frameScenes.Add(header.SceneFolders[i].RefID, header.SceneFolders[i]);
                frameBlocks[j++] = header.SceneFolders[i].RefID;
            }
            for (int i = 0; i != header.NumGeometries; i++)
            {
                FrameGeometry geo = new FrameGeometry(reader);
                frameGeometries.Add(geo.RefID, geo);
                frameBlocks[j++] = geo.RefID;
            }
            for (int i = 0; i != header.NumMaterialResources; i++)
            {
                FrameMaterial mat = new FrameMaterial(reader);
                frameMaterials.Add(mat.RefID, mat);
                frameBlocks[j++] = mat.RefID;
            }
            for (int i = 0; i != header.NumBlendInfos; i++)
            {
                FrameBlendInfo blendInfo = new FrameBlendInfo(reader);
                frameBlendInfos.Add(blendInfo.RefID, blendInfo);
                frameBlocks[j++] = blendInfo.RefID;
            }
            for (int i = 0; i != header.NumSkeletons; i++)
            {
                FrameSkeleton skeleton = new FrameSkeleton(reader);
                frameSkeletons.Add(skeleton.RefID, skeleton);
                frameBlocks[j++] = skeleton.RefID;
            }
            for (int i = 0; i != header.NumSkelHierachies; i++)
            {
                FrameSkeletonHierachy skeletonHierachy = new FrameSkeletonHierachy(reader);
                frameSkeletonHierachies.Add(skeletonHierachy.RefID, skeletonHierachy);
                frameBlocks[j++] = skeletonHierachy.RefID;
            }

            if (header.NumObjects > 0)
            {

                for (int i = 0; i != header.NumObjects; i++)
                    objectTypes[i] = reader.ReadInt32();

                for (int i = 0; i != header.NumObjects; i++)
                {
                    FrameObjectBase newObject = new FrameObjectBase();
                    if (objectTypes[i] == (int)ObjectType.Joint)
                        newObject = new FrameObjectJoint(reader);

                    else if (objectTypes[i] == (int)ObjectType.SingleMesh)
                    {
                        newObject = new FrameObjectSingleMesh(reader);
                        FrameObjectSingleMesh mesh = newObject as FrameObjectSingleMesh;
                        mesh.AddRef(frameBlocks[mesh.MeshIndex]);
                        mesh.AddRef(frameBlocks[mesh.MaterialIndex]);
                    }
                    else if (objectTypes[i] == (int)ObjectType.Frame)
                        newObject = new FrameObjectFrame(reader);

                    else if (objectTypes[i] == (int)ObjectType.Light)
                        newObject = new FrameObjectLight(reader);

                    else if (objectTypes[i] == (int)ObjectType.Camera)
                        newObject = new FrameObjectCamera(reader);

                    else if (objectTypes[i] == (int)ObjectType.Component_U00000005)
                        newObject = new FrameObjectComponent_U005(reader);

                    else if (objectTypes[i] == (int)ObjectType.Sector)
                        newObject = new FrameObjectSector(reader);

                    else if (objectTypes[i] == (int)ObjectType.Dummy)
                        newObject = new FrameObjectDummy(reader);

                    else if (objectTypes[i] == (int)ObjectType.ParticleDeflector)
                        newObject = new FrameObjectDeflector(reader);

                    else if (objectTypes[i] == (int)ObjectType.Area)
                        newObject = new FrameObjectArea(reader);

                    else if (objectTypes[i] == (int)ObjectType.Target)
                        newObject = new FrameObjectTarget(reader);

                    else if (objectTypes[i] == (int)ObjectType.Model)
                    {
                        FrameObjectModel mesh = new FrameObjectModel(reader);
                        mesh.ReadFromFile(reader);
                        mesh.ReadFromFilePart2(reader, frameSkeletons[frameBlocks[mesh.SkeletonIndex]], frameBlendInfos[frameBlocks[mesh.BlendInfoIndex]]);
                        mesh.AddRef(frameBlocks[mesh.MeshIndex]);
                        mesh.AddRef(frameBlocks[mesh.MaterialIndex]);
                        mesh.AddRef(frameBlocks[mesh.BlendInfoIndex]);
                        mesh.AddRef(frameBlocks[mesh.SkeletonIndex]);
                        mesh.AddRef(frameBlocks[mesh.SkeletonHierachyIndex]);
                        newObject = mesh;
                    }
                    else if (objectTypes[i] == (int)ObjectType.Collision)
                        newObject = new FrameObjectCollision(reader);

                    frameObjects[i] = newObject;
                }
            }
            DefineFrameBlockParents();
        }

        /// <summary>
        /// Writes the FrameResource to the file.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteToFile(BinaryWriter writer)
        {
            //BEFORE WE WRITE, WE NEED TO COMPILE AND UPDATE THE FRAME.
            UpdateFrameData();
            header.WriteToFile(writer);

            int totalBlockCount = header.NumFolderNames + header.NumGeometries + header.NumMaterialResources + header.NumBlendInfos + header.NumSkeletons + header.NumSkelHierachies;

            for (int i = 0; i != totalBlockCount; i++)
            {
                if (entireFrame[i].GetType() == typeof(FrameGeometry))
                    (entireFrame[i] as FrameGeometry).WriteToFile(writer);

                if (entireFrame[i].GetType() == typeof(FrameMaterial))
                    (entireFrame[i] as FrameMaterial).WriteToFile(writer);

                if (entireFrame[i].GetType() == typeof(FrameBlendInfo))
                    (entireFrame[i] as FrameBlendInfo).WriteToFile(writer);

                if (entireFrame[i].GetType() == typeof(FrameSkeleton))
                    (entireFrame[i] as FrameSkeleton).WriteToFile(writer);

                if (entireFrame[i].GetType() == typeof(FrameSkeletonHierachy))
                    (entireFrame[i] as FrameSkeletonHierachy).WriteToFile(writer);
            }

            for (int i = totalBlockCount; i != totalBlockCount+header.NumObjects; i++)
            {
                if (entireFrame[i].GetType() == typeof(FrameObjectJoint))
                    writer.Write((int)ObjectType.Joint);

                else if (entireFrame[i].GetType() == typeof(FrameObjectSingleMesh))
                    writer.Write((int)ObjectType.SingleMesh);

                else if (entireFrame[i].GetType() == typeof(FrameObjectFrame))
                    writer.Write((int)ObjectType.Frame);

                else if (entireFrame[i].GetType() == typeof(FrameObjectLight))
                    writer.Write((int)ObjectType.Light);

                else if (entireFrame[i].GetType() == typeof(FrameObjectCamera))
                    writer.Write((int)ObjectType.Camera);

                else if (entireFrame[i].GetType() == typeof(FrameObjectComponent_U005))
                    writer.Write((int)ObjectType.Component_U00000005);

                else if (entireFrame[i].GetType() == typeof(FrameObjectSector))
                    writer.Write((int)ObjectType.Sector);

                else if (entireFrame[i].GetType() == typeof(FrameObjectDummy))
                    writer.Write((int)ObjectType.Dummy);

                else if (entireFrame[i].GetType() == typeof(FrameObjectDeflector))
                    writer.Write((int)ObjectType.ParticleDeflector);

                else if (entireFrame[i].GetType() == typeof(FrameObjectArea))
                    writer.Write((int)ObjectType.Area);

                else if (entireFrame[i].GetType() == typeof(FrameObjectTarget))
                    writer.Write((int)ObjectType.Target);

                else if (entireFrame[i].GetType() == typeof(FrameObjectModel))
                    writer.Write((int)ObjectType.Model);

                else if (entireFrame[i].GetType() == typeof(FrameObjectCollision))
                    writer.Write((int)ObjectType.Collision);
            }

            for (int i = totalBlockCount; i != totalBlockCount + header.NumObjects; i++)
            {
                if (entireFrame[i].GetType() == typeof(FrameObjectJoint))
                    (entireFrame[i] as FrameObjectJoint).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectModel))
                    (entireFrame[i] as FrameObjectModel).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectSingleMesh))
                    (entireFrame[i] as FrameObjectSingleMesh).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectFrame))
                    (entireFrame[i] as FrameObjectFrame).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectLight))
                    (entireFrame[i] as FrameObjectLight).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectCamera))
                    (entireFrame[i] as FrameObjectCamera).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectComponent_U005))
                    (entireFrame[i] as FrameObjectComponent_U005).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectSector))
                    (entireFrame[i] as FrameObjectSector).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectDummy))
                    (entireFrame[i] as FrameObjectDummy).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectDeflector))
                    (entireFrame[i] as FrameObjectDeflector).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectArea))
                    (entireFrame[i] as FrameObjectArea).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectTarget))
                    (entireFrame[i] as FrameObjectTarget).WriteToFile(writer);

                else if (entireFrame[i].GetType() == typeof(FrameObjectCollision))
                    (entireFrame[i] as FrameObjectCollision).WriteToFile(writer);
            }
        }

        /// <summary>
        /// Adds names onto ParentIndex1 and ParentIndex2. Called after the file has been read.
        /// </summary>
        public void DefineFrameBlockParents()
        {
            UpdateEntireFrame();

            for (int i = 0; i != frameObjects.Length; i++)
            {

                FrameObjectBase newObject = frameObjects[i] as FrameObjectBase;

                if (newObject == null)
                    continue;

                if (newObject.ParentIndex1.Index > -1)
                    newObject.ParentIndex1.Name = (entireFrame[newObject.ParentIndex1.Index] as FrameObjectBase).Name.String;

                if (newObject.ParentIndex2.Index > -1)
                {
                    if (entireFrame[newObject.ParentIndex2.Index].GetType() == typeof(FrameHeaderScene))
                        newObject.ParentIndex2.Name = (entireFrame[newObject.ParentIndex2.Index] as FrameHeaderScene).Name.String;
                    else
                        newObject.ParentIndex2.Name = (entireFrame[newObject.ParentIndex2.Index] as FrameObjectBase).Name.String;
                }

                frameObjects[i] = newObject;
            }
        }

        /// <summary>
        /// This builds the entire frame. This is called after the file has been read.
        /// </summary>
        public void UpdateEntireFrame()
        {
            entireFrame = new List<object>(frameBlocks.Length + frameObjects.Length);

            foreach (object block in frameBlocks)
                entireFrame.Add(block);

            foreach (object objects in frameObjects)
                entireFrame.Add(objects);
        }

        /// <summary>
        /// This reconstructs the data.
        /// </summary>
        public void UpdateFrameData()
        {
            int totalResources = header.NumFolderNames + header.NumGeometries + header.NumMaterialResources + header.NumBlendInfos + header.NumSkeletons + header.NumSkelHierachies;


            Dictionary<int, object> newFrame = new Dictionary<int, object>();
            //TODO SCENES.
            foreach (KeyValuePair<int, FrameGeometry> entry in frameGeometries)
            {
                newFrame.Add(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<int, FrameMaterial> entry in frameMaterials)
            {
                newFrame.Add(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<int, FrameBlendInfo> entry in frameBlendInfos)
            {
                newFrame.Add(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<int, FrameSkeleton> entry in frameSkeletons)
            {
                newFrame.Add(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<int, FrameSkeletonHierachy> entry in frameSkeletonHierachies)
            {
                newFrame.Add(entry.Key, entry.Value);
            }

            header.NumFolderNames = 0;
            header.NumGeometries = 0;
            header.NumMaterialResources = 0;
            header.NumBlendInfos = 0;
            header.NumSkeletons = 0;
            header.NumSkelHierachies = 0;
            header.NumObjects = 0;

            for (int i = 0; i != frameObjects.Length; i++)
            {
                object block = frameObjects[i];

                if (block.GetType() == typeof(FrameObjectSingleMesh))
                {
                    FrameObjectSingleMesh mesh = (block as FrameObjectSingleMesh);
                    mesh.MaterialIndex = newFrame.IndexOfValue(frameBlocks[mesh.MaterialIndex]);
                    mesh.MeshIndex = newFrame.IndexOfValue(frameBlocks[mesh.MeshIndex]);
                    newFrame.Add(mesh.RefID, mesh);
                }
                else if (block.GetType() == typeof(FrameObjectModel))
                {
                    FrameObjectModel mesh = (block as FrameObjectModel);
                    mesh.MaterialIndex = newFrame.IndexOfValue(frameBlocks[mesh.MaterialIndex]);
                    mesh.MeshIndex = newFrame.IndexOfValue(frameBlocks[mesh.MeshIndex]);
                    mesh.BlendInfoIndex = newFrame.IndexOfValue(frameBlocks[mesh.BlendInfoIndex]);
                    mesh.SkeletonIndex = newFrame.IndexOfValue(frameBlocks[mesh.SkeletonIndex]);
                    mesh.SkeletonHierachyIndex = frameSkeletonHierachies.IndexOfValue(frameBlocks[mesh.SkeletonHierachyIndex]);
                    newFrame.Add(mesh.RefID, mesh);
                }
                else
                {
                    newFrame.Add((block as FrameEntry).RefID, block);
                }
            }
            entireFrame = newFrame.Values.ToList();
            for (int i = 0; i != entireFrame.Count; i++)
            {
                object block = entireFrame[i];

                if (block.GetType() == typeof(FrameHeaderScene))
                    header.NumFolderNames++;
                else if (block.GetType() == typeof(FrameGeometry))
                    header.NumGeometries++;
                else if (block.GetType() == typeof(FrameMaterial))
                    header.NumMaterialResources++;
                else if (block.GetType() == typeof(FrameBlendInfo))
                    header.NumBlendInfos++;
                else if (block.GetType() == typeof(FrameSkeleton))
                    header.NumSkeletons++;
                else if (block.GetType() == typeof(FrameSkeletonHierachy))
                    header.NumSkelHierachies++;
                else if (block.GetType() == typeof(FrameObjectJoint))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectModel))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectSingleMesh))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectFrame))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectLight))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectCamera))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectComponent_U005))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectSector))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectDummy))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectDeflector))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectArea))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectTarget))
                    header.NumObjects++;
                else if (block.GetType() == typeof(FrameObjectCollision))
                    header.NumObjects++;
            }
        }
    }
}
