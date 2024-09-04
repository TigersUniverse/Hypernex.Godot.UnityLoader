dotnet build -c Debug

Copy-Item -Path .godot/mono/temp/bin/Debug/AssetRipper.TextureDecoder.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/AssetsTools.NET.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/AssetsTools.NET.Texture.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/Fmod5Sharp.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/Hypernex.Godot.UnityLoader.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/Hypernex.Godot.UnityLoader.pdb -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/IndexRange.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/Debug/OggVorbisEncoder.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
