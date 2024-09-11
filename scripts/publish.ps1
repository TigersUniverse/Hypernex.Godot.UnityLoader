dotnet build -c ExportRelease Hypernex.Godot.UnityLoader.csproj

Copy-Item -Path .godot/mono/temp/bin/ExportRelease/AssetRipper.TextureDecoder.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/AssetsTools.NET.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/AssetsTools.NET.Texture.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/Fmod5Sharp.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/Hypernex.Godot.UnityLoader.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/Hypernex.Godot.UnityLoader.pdb -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/IndexRange.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
Copy-Item -Path .godot/mono/temp/bin/ExportRelease/OggVorbisEncoder.dll -Destination $env:APPDATA/Godot/app_userdata/Hypernex.Godot/Plugins -Force
