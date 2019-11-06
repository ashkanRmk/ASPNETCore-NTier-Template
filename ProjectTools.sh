#!/bin/bash
clear

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $DIR

unamestr=`uname`


PROJ_NAME="Liaro"         #Project Name

while true
do
    clear
    echo -e "\e[31mPlease enter your choice:\e[0m \n"
    echo "1) Add migration"
    echo "2) Make migration script"
    echo "3) Clean migrations log"
    echo "4) Quit"
    echo ""
    read -p "-> " opt
    case $opt in
    1)
        clear
        cd ./src/$PROJ_NAME
        today=`date +%Y-%m-%d.%H:%M:%S`

        echo "Enter your migration name:"
        read MIG

        dotnet ef migrations add $MIG

        cd Migrations

        touch script.txt
        echo "dotnet ef migrations add "$MIG >> script.txt
        echo "" >> script.txt
        echo "Created Time: "$today >> script.txt
        echo "-----------------------------------" >> script.txt

        echo
        cd ../../..
        echo -e "\e[1mPREE ENTER TO CONTINUE ...\e[0m"
        read
        ;;
    2)
        clear
        cd ./src/$PROJ_NAME

        echo "Enter your NEWLY migration name completely:"
        read TO
        echo
        echo "Enter your LAST migration name completely: (if first time enter 0)"
        read FROM

        if [ ! -d ./Scripts ]; then
        mkdir Scripts
        fi
        name=$(echo $TO | cut -f 1 -d '.')
        today=`date +%Y-%m-%d.%H:%M:%S`


        dotnet ef migrations script $FROM $TO -o ./Migrations/Scripts/$name.sql

        cd Migrations

        touch script.txt
        echo "dotnet ef migrations script "$FROM" "$TO" -o ./Migrations/Scripts/"$name".sql" >> script.txt
        echo "" >> script.txt
        echo "Created Time: "$today >> script.txt
        echo "-----------------------------------" >> script.txt

        echo
        cd ../../..
        echo -e "\e[1mPREE ENTER TO CONTINUE ...\e[0m"
        read
        ;;
    3)
        clear
        cd ./src/$PROJ_NAME/Migrations
        > script.txt
        echo "Migration logs Cleaned!"
        echo
        cd ../../..
        echo -e "\e[1mPREE ENTER TO CONTINUE ...\e[0m"
        read
        ;;
    4)
        clear
        break
        ;;
    *)
        clear
        echo invalid option
        ;;
    esac
done

