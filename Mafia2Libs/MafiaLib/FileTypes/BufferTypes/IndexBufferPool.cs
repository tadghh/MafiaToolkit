﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils.Extensions;

namespace ResourceTypes.BufferPools
{
    public class IndexBufferManager
    {
        List<FileInfo> loadedPoolNames;
        Dictionary<ulong, IndexBuffer> buffers;
        DirectoryInfo root;

        public Dictionary<ulong, IndexBuffer> Buffers {
            get { return buffers; }
        }

        public List<FileInfo> LoadedPoolNames {
            get { return loadedPoolNames; }
        }

        public IndexBufferManager(List<FileInfo> files, bool isBigEndian = false)
        {
            loadedPoolNames = files;
            buffers = new Dictionary<ulong, IndexBuffer>();
            if (LoadedPoolNames.Count > 0)
            {
                root = loadedPoolNames[0].Directory;
            }
            ReadFiles(isBigEndian);
        }

        public bool AddBuffer(IndexBuffer buffer)
        {
            if(!HasBuffer(buffer))
            {
                buffers.Add(buffer.Hash, buffer);
                return true;
            }
            return false;
        }

        public bool HasBuffer(ulong hash)
        {
            return buffers.ContainsKey(hash);
        }

        public bool HasBuffer(IndexBuffer buffer)
        {
            return HasBuffer(buffer.Hash);
        }

        public bool RemoveBuffer(IndexBuffer buffer)
        {
            if(HasBuffer(buffer))
            {
                buffers.Remove(buffer.Hash);
                return true;
            }
            return false;
        }

        public bool RemoveBuffer(ulong hash)
        {
            if (HasBuffer(hash))
            {
                buffers.Remove(hash);
                return true;
            }
            return false;
        }

        public bool ReplaceBuffer(IndexBuffer buffer)
        {
            if(HasBuffer(buffer))
            {
                buffers[buffer.Hash] = buffer;
                return true;
            }
            return false;
        }

        public void ReadFiles(bool isBigEndian)
        {
            for (int i = 0; i != loadedPoolNames.Count; i++)
            {
                IndexBufferPool pool = null;
                using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(loadedPoolNames[i].FullName), false))
                {
                    pool = new IndexBufferPool(stream, isBigEndian);
                }

                foreach (var buff in pool.Buffers)
                {
                    if (!buffers.ContainsKey(buff.Key))
                    {
                        buffers.Add(buff.Key, buff.Value);
                    }
                    else
                    {
                        Console.WriteLine("Skipped a buffer {0}", buff.Key);
                    }
                }
            }
        }

        public IndexBuffer GetBuffer(ulong indexRef)
        {
            return buffers.ContainsKey(indexRef) ? buffers[indexRef] : null;
        }

        public void WriteToFile(bool isBigEndian = false)
        {
            int numPool = 0;
            int poolSize = 0;
            IndexBufferPool pool = new IndexBufferPool();

            foreach(var loaded in loadedPoolNames)
            {
                File.Delete(loaded.FullName);
            }
            loadedPoolNames.Clear();
            var buffArray = buffers.Values.ToArray();
            //var sorted = buffArray.OrderBy(buff => buff.Data.Length);
            foreach (var buffer in buffArray)
            {
                int prePoolSize = (poolSize) + (int)(buffer.GetLength());
                if (pool.Buffers.Count == 128 || prePoolSize > 920000)
                {
                    string name = Path.Combine(root.FullName, string.Format("IndexBufferPool_{0}.ibp", numPool));
                    SavePool(pool, name, isBigEndian);
                    pool = new IndexBufferPool();
                    numPool++;
                    prePoolSize = 0;
                }

                pool.Buffers.Add(buffer.Hash, buffer);
                poolSize = prePoolSize;
            }

            if(pool != null)
            {
                string name = Path.Combine(root.FullName, string.Format("IndexBufferPool_{0}.ibp", numPool));
                SavePool(pool, name, isBigEndian);
            }           
        }

        private void SavePool(IndexBufferPool pool, string name, bool isBigEndian)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                pool.WriteToFile(stream, isBigEndian);              
                File.WriteAllBytes(name, stream.ToArray());
                loadedPoolNames.Add(new FileInfo(name));
            }
        }
    }

    public class IndexBufferPool
    {
        //MAX BUFFER SIZE IS 128
        private BufferType version;
        private int numBuffers;
        private uint size;
        private Dictionary<ulong, IndexBuffer> buffers = new Dictionary<ulong, IndexBuffer>();

        public Dictionary<ulong, IndexBuffer> Buffers {
            get { return buffers; }
        }

        public IndexBufferPool(MemoryStream stream, bool isBigEndian)
        {
            ReadFromFile(stream, isBigEndian);
        }

        public IndexBufferPool()
        {
            version = BufferType.Index;
            numBuffers = 0;
            size = 0;
            buffers = new Dictionary<ulong, IndexBuffer>();
        }

        public void ReadFromFile(MemoryStream stream, bool isBigEndian)
        {
            version = (BufferType)stream.ReadByte8();
            numBuffers = stream.ReadInt32(isBigEndian);
            size = stream.ReadUInt32(isBigEndian);

            for (int i = 0; i != numBuffers; i++)
            {
                IndexBuffer buffer = new IndexBuffer(stream, isBigEndian);

                if (!buffers.ContainsKey(buffer.Hash))
                {
                    buffers.Add(buffer.Hash, buffer);
                }
            }
        }

        public void WriteToFile(MemoryStream stream, bool isBigEndian)
        {
            size = 0;

            stream.WriteByte((byte)version);
            stream.Write(buffers.Count, isBigEndian);

            //need to make sure we update total size of buffers.
            for (int i = 0; i != buffers.Count; i++)
            {
                uint usage = (uint)buffers.ElementAt(i).Value.GetLength();
                size += usage;
            }
            size += 128;
            stream.Write(size, isBigEndian);

            for (int i = 0; i != buffers.Count; i++)
            {
                buffers.ElementAt(i).Value.WriteToFile(stream, isBigEndian);
            }
        }
    }

    public class IndexBuffer
    {
        private ulong hash;
        private int indexFormat;
        private uint length;
        private uint[] data;

        public ulong Hash {
            get { return hash; }
            set { hash = value; }
        }

        public IndexBuffer(ulong hash)
        {
            this.hash = hash;
            this.indexFormat = 1;
        }

        public IndexBuffer(MemoryStream stream, bool isBigEndian)
        {
            ReadFromFile(stream, isBigEndian);
        }

        public void ReadFromFile(MemoryStream stream, bool isBigEndian)
        {
            hash = stream.ReadUInt64(isBigEndian);
            indexFormat = stream.ReadInt32(isBigEndian);
            length = stream.ReadUInt32(isBigEndian);

            if (indexFormat == 1)
            {
                data = new uint[length / 2];

                int num = 0;
                int index = 0;

                while (num < length)
                {
                    data[index] = stream.ReadUInt16(isBigEndian);
                    num += 2;
                    ++index;
                }
            }
            else
            {
                data = new uint[length / 4];

                int num = 0;
                int index = 0;

                while (num < length)
                {
                    data[index] = stream.ReadUInt32(isBigEndian);
                    num += 4;
                    ++index;
                }
            }
        }
        
        public void SwapIndexFormat()
        {
            indexFormat = (indexFormat == 1 ? 2 : 1);
            GetLength();
        }

        public void WriteToFile(MemoryStream stream, bool isBigEndian = false)
        {
            stream.Write(hash, isBigEndian);
            stream.Write(indexFormat, isBigEndian);
            GetLength();
            stream.Write(length, isBigEndian);

            if (indexFormat == 1)
            {
                for (int i = 0; i != data.Length; i++)
                {
                    stream.Write((ushort)data[i], isBigEndian);
                }
            }
            else
            {
                for (int i = 0; i != data.Length; i++)
                {
                    stream.Write(data[i], isBigEndian);
                }
            }
        }

        public uint[] GetData()
        {
            return data;
        }

        public uint GetLength()
        {
            length = (uint)(indexFormat == 2 ? data.Length * 4 : data.Length * 2);
            return length;
        }

        public void SetData(uint[] data)
        {
            this.data = data;
            length = (uint)(indexFormat == 2 ? data.Length * 4 : data.Length * 2);
        }
    }
}
