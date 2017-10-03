on run argv
    set l to {}

    read (item 1 of argv) as «class utf8» using delimiter linefeed

    tell application "Finder"
        repeat with p in result
            if exists p as POSIX file
                set x to (p as POSIX file)

                set l to l & x
            end if
        end repeat

        delete l
    end tell

end run
