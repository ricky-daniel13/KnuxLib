﻿using FraGag.Compression;

namespace KnuxLib.Engines.NiGHTS2
{
    public class ONE : FileBase
    {
        // Generic VS stuff to allow creating an object that instantly loads a file.
        public ONE() { }
        public ONE(string filepath, bool extract = false)
        {
            Load(filepath);

            if (extract)
                Extract($@"{Path.GetDirectoryName(filepath)}\{Path.GetFileNameWithoutExtension(filepath)}");
        }

        // Actual data presented to the end user.
        public List<FileNode> Data = new();

        /// <summary>
        /// Loads and parses this format's file.
        /// </summary>
        /// <param name="filepath">The path to the file to load and parse.</param>
        public override void Load(string filepath)
        {
            // Set up Marathon's BinaryReader.
            BinaryReaderEx reader = new(File.OpenRead(filepath));

            // Read this file's signature.
            reader.ReadSignature(0x0D, "ThisIsOneFile");

            // Realign to 0x10 bytes.
            reader.FixPadding(0x10);

            // Read an unknown value that is usually 0xCC, but can be 0xCA or 0xCB.
            // TODO: Does this value matter?
            uint UnknownUInt32_1 = reader.ReadUInt32();

            // Read this archive's identifier string, usually `Default`, but sometimes `landData`.
            // TODO: Does this value matter?
            string ArchiveIdentifier = reader.ReadNullPaddedString(0x20);

            // Read the amount of files in this archive.
            uint fileCount = reader.ReadUInt32();

            // Skip 0x84 bytes. This chunk is always one value of 0x20 then 0x80 null bytes.
            reader.JumpAhead(0x84);

            // Read each file.
            for (int i = 0; i < fileCount; i++)
            {
                // Set up a new node.
                FileNode node = new();

                // Skip a value that appears to be this file's index.
                reader.JumpAhead(0x04);

                // Read this file's compressed size.
                int compressedSize = reader.ReadInt32();

                // Read a value that determines if this file is compressed.
                bool compressed = reader.ReadBoolean(0x04);

                // Read this file's uncompressed size. This will be 0 if the file isn't compressed, in which case compressedSize is the actual size.
                uint uncompressedSize = reader.ReadUInt32();

                // Skip another value that appears to be this file's index.
                reader.JumpAhead(0x04);

                // Read the offset to this file's data.
                uint fileOffset = reader.ReadUInt32();

                // Read this file's name.
                node.Name = reader.ReadNullPaddedString(0x80);

                // Ignore 0x40 bytes that are usually nulls, but some files have.
                // TODO: I don't like skipping data, what are these?
                reader.JumpAhead(0x40);

                // Save our current position so we can jump back for the next file.
                long position = reader.BaseStream.Position;

                // Jump to this file's data location.
                reader.JumpTo(fileOffset);

                // Read this file's data, decompressing if needed.
                if (compressed)
                {
                    // Print the name of the file we're deccompressing.
                    Console.WriteLine($"Decompressing {node.Name}.");

                    // Read and decompress this file's data.
                    node.Data = Prs.Decompress(reader.ReadBytes(compressedSize));
                }
                else
                {
                    // Read this file's uncompressed data.
                    node.Data = reader.ReadBytes(compressedSize);
                }

                // Jump back for the next file.
                reader.JumpTo(position);

                // Save this node.
                Data.Add(node);
            }

            // TODO: Verify I haven't missed anything.

            // Close Marathon's BinaryReader.
            reader.Close();
        }

        /// <summary>
        /// Saves this format's file.
        /// TODO: Do I need to worry about the UnknownUInt32 and the ArchiveIdentifier?
        /// TODO: Do I need to worry about compressing files?
        /// TODO: Do I need to worry about those 0x40 bytes after the file name?
        /// </summary>
        /// <param name="filepath">The path to save to.</param>
        public void Save(string filepath)
        {
            // Set up Marathon's BinaryWriter.
            BinaryWriterEx writer = new(File.Create(filepath));

            // Write the file signature.
            writer.WriteNullPaddedString("ThisIsOneFile", 0x10);

            // Write this file's unknown integer value.
            writer.Write(0xCC);

            // Write the `Default` ArchiveIdentifier.
            writer.WriteNullPaddedString("Default", 0x20);

            // Write the amount of files in this archive.
            writer.Write(Data.Count);

            // Write an unknown integer value of 0x20.
            writer.Write(0x20);

            // Write 0x80 null bytes.
            writer.WriteNulls(0x80);

            // Loop through and write each file's information.
            for (int i = 0; i < Data.Count; i++)
            {
                // Write this file's index.
                writer.Write(i);

                // Write this file's length.
                writer.Write(Data[i].Data.Length);

                // Write this file's compressed flag.
                writer.WriteBoolean32(false);

                // Write a dummy uncompressed size.
                writer.Write(0);

                // Write this file's index again.
                writer.Write(i);

                // Add an offset for this file's data.
                writer.AddOffset($"File{i}Offset");

                // Write this file's name.
                writer.WriteNullPaddedString(Data[i].Name, 0x80);

                // Write 0x40 null bytes.
                writer.WriteNulls(0x40);
            }

            // Loop through and write each file's data.
            for (int i = 0; i < Data.Count; i++)
            {
                // Realign to 0x20 bytes.
                writer.FixPadding(0x20);

                // Fill in the offset for this file's data.
                writer.FillOffset($"File{i}Offset");

                // Write this file's data.
                writer.Write(Data[i].Data);
            }

            // Close Marathon's BinaryWriter.
            writer.Close();
        }

        /// <summary>
        /// Extracts the files in this format to disc.
        /// </summary>
        /// <param name="directory">The directory to extract to.</param>
        public void Extract(string directory)
        {
            // Create the extraction directory.
            Directory.CreateDirectory(directory);

            // Loop through each node to extract.
            foreach (FileNode node in Data)
            {
                // Print the name of the file we're extracting.
                Console.WriteLine($"Extracting {node.Name}.");

                // Some files in Journey of Dreams have a full drive path in them, trying to extract them fails, so replace the colon with an indicator as a workaround.
                if (node.Name.Contains(':'))
                    node.Name = node.Name.Replace(":", "[COLON]");

                // The NiGHTS 2 Engine can use sub directories in its archives. Create the directory if needed.
                if (!Directory.Exists($@"{directory}\{Path.GetDirectoryName(node.Name)}"))
                    Directory.CreateDirectory($@"{directory}\{Path.GetDirectoryName(node.Name)}");

                // Extract the file.
                File.WriteAllBytes($@"{directory}\{node.Name}", node.Data);
            }
        }

        /// <summary>
        /// Imports files from a directory into a ONE node.
        /// TODO: This seemed to work, but hasn't been extensively tested at all.
        /// </summary>
        /// <param name="directory">The directory to import, including sub directories.</param>
        public void Import(string directory)
        {
            foreach (string file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                FileNode node = new()
                {
                    Name = file.Replace($@"{directory}\", ""),
                    Data = File.ReadAllBytes(file)
                };

                if (node.Name.Contains("[COLON]"))
                    node.Name = node.Name.Replace("[COLON]", ":");

                Data.Add(node);
            }
        }
    }
}
