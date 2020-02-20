module Arrays
    
    let split (array: 'T[], groups: int): 'T[][] =
        array |> Array.splitInto groups

    let rec equalPartLengths (len: int, groups: int): list<int> =
        if groups <= 1 then
            [len]
        else 
            let division = (len / groups) + 1
            division::equalPartLengths(len - division, groups - 1)

    let getPartIndicesRange (parts: int[], rank: int): struct (int * int) =
        let first = parts |> Array.take rank |> Array.sum
        let last = first + parts.[rank] - 1

        struct (first, last)