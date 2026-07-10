# Usage:
#   ./scripts/fetch-db.sh "user@server.domain.tld" "/var/www/www.domain.tld"

export SOURCE_SERVER=$1
export SOURCE_PATH=$2
export TARGET_PATH=$3

rsync -av --info=progress2 $SOURCE_SERVER:$SOURCE_PATH/WaterAlarm.db $TARGET_PATH/WaterAlarm.db
