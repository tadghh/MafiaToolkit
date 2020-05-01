﻿using SharpDX;
using System;
using System.IO;
using Utils.Extensions;

namespace Utils.SharpDXExtensions
{
    public static class MatrixExtensions
    {
        public static bool IsNaN(this Matrix matrix)
        {
            return matrix.Column1.IsNaN() || matrix.Column2.IsNaN() || matrix.Column3.IsNaN() || matrix.Column4.IsNaN();
        }

        public static Matrix ReadFromFile(BinaryReader reader)
        {
            Matrix matrix = new Matrix();
            matrix.Column1 = Vector4Extenders.ReadFromFile(reader);
            matrix.Column2 = Vector4Extenders.ReadFromFile(reader);
            matrix.Column3 = Vector4Extenders.ReadFromFile(reader);
            return matrix;
        }

        public static Matrix ReadFromFile(MemoryStream stream, bool isBigEndian)
        {
            Matrix matrix = new Matrix();
            matrix.Column1 = Vector4Extenders.ReadFromFile(stream, isBigEndian);
            matrix.Column2 = Vector4Extenders.ReadFromFile(stream, isBigEndian);
            matrix.Column3 = Vector4Extenders.ReadFromFile(stream, isBigEndian);
            if(matrix.IsNaN())
            {
                matrix.Row1 = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                matrix.Row2 = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
                matrix.Row3 = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            }
            return matrix;
        }

        public static void WriteToFile(this Matrix matrix, BinaryWriter writer)
        {
            Vector4Extenders.WriteToFile(matrix.Column1, writer);
            Vector4Extenders.WriteToFile(matrix.Column2, writer);
            Vector4Extenders.WriteToFile(matrix.Column3, writer);
        }

        public static void WriteToFile(this Matrix matrix, MemoryStream stream, bool isBigEndian)
        {
            Vector4Extenders.WriteToFile(matrix.Column1, stream, isBigEndian);
            Vector4Extenders.WriteToFile(matrix.Column2, stream, isBigEndian);
            Vector4Extenders.WriteToFile(matrix.Column3, stream, isBigEndian);
        }

        public static Matrix SetMatrix(Quaternion rotation, Vector3 scale, Vector3 position)
        {
            Matrix r = Matrix.RotationQuaternion(rotation);
            Matrix s = Matrix.Scaling(scale);
            Matrix t = Matrix.Translation(position);
            return t * r * s;
        }

        public static Matrix SetTranslationVector(Matrix other, Vector3 position)
        {
            Matrix matrix = other;
            matrix.TranslationVector = position;
            return matrix;
        }

        public static Matrix RowEchelon(Matrix other)
        {
            Matrix matrix = new Matrix();
            matrix.Row1 = other.Column1;
            matrix.Row2 = other.Column2;
            matrix.Row3 = other.Column3;
            matrix.Row4 = other.Row4;
            matrix.Column4 = Vector4.Zero;
            return matrix;
        }

        public static Vector3 RotatePoint(this Matrix matrix, Vector3 position)
        {
            Vector3 vector = new Vector3();
            vector.X = matrix.M11 * position.X + matrix.M12 * position.Y + matrix.M13 * position.Z;
            vector.Y = matrix.M21 * position.X + matrix.M22 * position.Y + matrix.M23 * position.Z;
            vector.Z = matrix.M31 * position.X + matrix.M32 * position.Y + matrix.M33 * position.Z;
            return vector; //Output coords in order (x,y,z)
        }

        public static Matrix SetMatrix(Vector3 rotation, Vector3 scale, Vector3 position)
        {
            float radX, radY, radZ;
            radX = MathUtil.DegreesToRadians(rotation.X);
            radY = MathUtil.DegreesToRadians(rotation.Y);
            radZ = MathUtil.DegreesToRadians(rotation.Z);
            Quaternion qX = Quaternion.RotationAxis(Vector3.UnitX, radX);
            Quaternion qY = Quaternion.RotationAxis(Vector3.UnitY, radY);
            Quaternion qZ = Quaternion.RotationAxis(Vector3.UnitZ, radZ);
            return SetMatrix(qX * qY * qZ, scale, position);
        }
    }

    public static class QuaternionExtensions
    {
        public static Vector3 ToEuler(this Quaternion quat)
        {
            Vector3 euler = new Vector3();
            var qw = quat.W;
            var qx = quat.X;
            var qy = quat.Y;
            var qz = quat.Z;
            var eX = Math.Atan2(-2 * ((qy * qz) - (qw * qx)), (qw * qw) - (qx * qx) - (qy * qy) + (qz * qz));
            var eY = Math.Asin(2 * ((qx * qz) + (qw * qy)));
            var eZ = Math.Atan2(-2 * ((qx * qy) - (qw * qz)), (qw * qw) + (qx * qx) - (qy * qy) - (qz * qz));
            euler.Z = (float)Math.Round(eZ * 180 / Math.PI);
            euler.Y = (float)Math.Round(eY * 180 / Math.PI);
            euler.X = (float)Math.Round(eX * 180 / Math.PI);

            if(euler.IsNaN())
            {
                throw new Exception("Triggered NaN check in QuaternionExtensions.ToEuler();");
            }

            return euler;
        }

        public static Quaternion ReadFromFile(BinaryReader reader)
        {
            Quaternion quaternion = new Quaternion();
            quaternion.X = reader.ReadSingle();
            quaternion.Y = reader.ReadSingle();
            quaternion.Z = reader.ReadSingle();
            quaternion.W = reader.ReadSingle();
            return quaternion;
        }

        public static Quaternion ReadFromFile(MemoryStream stream, bool isBigEndian)
        {
            Quaternion quaternion = new Quaternion();
            quaternion.X = stream.ReadSingle(isBigEndian);
            quaternion.Y = stream.ReadSingle(isBigEndian);
            quaternion.Z = stream.ReadSingle(isBigEndian);
            quaternion.W = stream.ReadSingle(isBigEndian);
            return quaternion;
        }

        public static void WriteToFile(this Quaternion quaternion, BinaryWriter writer)
        {
            writer.Write(quaternion.X);
            writer.Write(quaternion.Y);
            writer.Write(quaternion.Z);
            writer.Write(quaternion.W);
        }

        public static void WriteToFile(this Quaternion quaternion, MemoryStream stream, bool isBigEndian)
        {
            stream.Write(quaternion.X, isBigEndian);
            stream.Write(quaternion.Y, isBigEndian);
            stream.Write(quaternion.Z, isBigEndian);
            stream.Write(quaternion.W, isBigEndian);
        }
    }
}
