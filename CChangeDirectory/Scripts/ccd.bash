ccd() {
r=$(cchangedirectory $*)
read -ra arr <<< "$r"
if [[ ${arr[0]} = "#!cd" ]]; then
    cd ${arr[1]}
else
    echo $r
fi
}