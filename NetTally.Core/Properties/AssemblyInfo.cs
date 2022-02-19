using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Allow the unit test project access to 'internal' classes in the core library.
[assembly: InternalsVisibleTo("TallyUnitTest")]
[assembly: InternalsVisibleTo("NetTally.Tests")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("037e6032-88e9-4861-ab1f-42ec39859ecb")]
