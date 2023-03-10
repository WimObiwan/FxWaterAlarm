# Usage:
#   ./scripts/deploy.sh "net6.0" "user@server.domain.tld" "/var/www/www.domain.>

export RELEASE=$1
export TARGET_SERVER=$2
export TARGET_PATH=$3
export TARGET_SERVICE=$4
export TARGET_PATH_CONSOLE=$5

dotnet publish --configuration Release
rsync -av --info=progress2 --exclude=appsettings.Local.json ./Site/bin/Release/$RELEASE/publish/* $TARGET_SERVER:$TARGET_PATH/
rsync -av --info=progress2 --exclude=appsettings.Local.json ./Console/bin/Release/$RELEASE/publish/* $TARGET_SERVER:$TARGET_PATH_CONSOLE/
ssh $TARGET_SERVER "service $TARGET_SERVICE restart"
