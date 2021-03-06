#!/bin/bash

dotnet pack ./src/shared/UdpToolkit/UdpToolkit.csproj                                                                       -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Annotations/UdpToolkit.Annotations.csproj                                               -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Framework/UdpToolkit.Framework.csproj                                                   -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Framework.CodeGenerator.Contracts/UdpToolkit.Framework.CodeGenerator.Contracts.csproj   -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Framework.Contracts/UdpToolkit.Framework.Contracts.csproj                               -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Logging/UdpToolkit.Logging.csproj                                                       -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Network/UdpToolkit.Network.csproj                                                       -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Network.Contracts/UdpToolkit.Network.Contracts.csproj                                   -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/shared/UdpToolkit.Serialization/UdpToolkit.Serialization.csproj                                           -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages

dotnet pack ./src/tools/UdpToolkit.CodeGenerator/UdpToolkit.CodeGenerator.csproj  -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages
dotnet pack ./src/tools/UdpToolkit.Cli/UdpToolkit.Cli.csproj                      -p:PackageVersion=$PACKAGE_VERSION --configuration Release -o ./packages