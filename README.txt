CONTENTS OF THIS FILE
---------------------

 * Introduction
 * Building

INTRODUCTION
------------

RiveSharpSample.sln contains the Rive C# runtime, along with examples on how
to use it:

  Viewer.csproj: A simple UWP app that draws 4 .riv files with pointer events.

  StateMachineInputs.csproj: A simple UWP app that draws a remote .riv file and
                             data binds state machine inputs to XAML controls.

  Goldens.csproj: A console app that renders images for testing

BUILDING
------------

==== In the public "rive-sharp" repo ====

You just need to fetch the rive-cpp submodule:

 git submodule update --init

After that, you should be able to open RiveSharpSample.sln in Visual Studio 2022
community, build, and run!

==== In the internal "rive" repo ====

To build, you first need to generate rive.vcproj, the project that builds the
native rive.dll:

  cd Native
  premake5.exe vs2022

Once rive.vcproj is generated, you should be able to open RiveSharpSample.sln in
Visual Studio 2022 community, build, and run!
