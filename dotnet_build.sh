rm -rf /home/jenkins/AssemblyOutput/MemberSite

mkdir /home/jenkins/AssemblyOutput/MemberSite

dotnet build MemberSite/MemberSite.sln --configuration Release --output /home/jenkins/AssemblyOutput/MemberSite

Tag=$(git rev-parse --short HEAD)

zip -r /home/jenkins/Release/MemberSite-$Tag /home/jenkins/AssemblyOutput/MemberSite/*