# BlobHandles
_"Blob Handles"_ are a fast & easy way to hash and compare segments of memory in C# / Unity.

They allow you to do two main things:
1) Use a sequence of bytes as a hash key, like in a `Dictionary<BlobHandle, T>`.
2) Quickly compare two handles' slices of memory for equality

## Blob Strings
This also includes _BlobString_, a wrapper around `BlobHandle` that points to an unmanaged representation of a string. 

_BlobString_ is designed for use cases that involve reading strings from an unmanaged source (network / disk) & comparing them against some sort of hash set.  It was written to power an [OSC parser](https://github.com/stella3d/OscCore).



