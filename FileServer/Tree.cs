﻿namespace FileServer;

// This structure represents the folder structure
public class Tree
{
    public string Value { get; set; } = string.Empty;
    public List<Tree> Nodes { get; set; } = new List<Tree>();
}