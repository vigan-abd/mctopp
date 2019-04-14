# MCTOPP -> Multi Constraint Team Orienteering Problem with Patterns

## Problem definition
Given an array of POIs (Points of Interests) and a number of Tours M find a solution that passes all hard constraints.

Hard Constraints:
* Budget ~ Sum of costs of all POIs inside solution (not single Tour) must not exceed total budget 
* Patterns ~ Each tour should match a specific pattern of POI types. Additonal POIs may be inserted between, before or after POIs that match the pattern. E.g. pattern for i-th tour: `3,2,4,8`. Valid tour may have these POI types: `{1,3,2,2,4,6,6,4,8,8,1}`.
* Time budget ~ Time budget is defined by difference between closing and opening time of the starting point (POI[0]). All POI durations + traveling time between POIs must not exceed the time budget. Traveling time between two POIs is calculated as euclidean distance between them.
* Max number of POI types - For each POI type a maximum allowed number of items that can be present in solution is defined. E.g. max 3 POIs of type 7 can be present in solution.
* A POI can't be visited more than once


## Solution representation
M -> number of tours
Array of M with linked list of POIs, e.g. M = 2 then solution is: 
```
{[3,8,15,2],[4,9,17]}
```


## Evaluation function
```
E(S) = SUM(POI[i, j].score)
i ~> i-th tour
j ~> j-th element inside i-th tour
```

## Initial solution construction
As first step in initial solution construction we can do preprocessing:
* Discart POIs that have high variance in duration, budget or avg distance against other POIs. The idea here is to discart these POIs since they will fill a lot of space in tour and reduce the number of POIs that can be present in solution. If we remove these POIs then we also reduce search space.

Once we discart the malicious POIs then we can apply one of the following strategies to select pivot POIs for matching the patterns:
* Group POIs of type that match the pattern and then choose the ones that have highest satisfaction factor. E.g. if we have pattern `3,2,4,8` we sort POIs of type 3 by score and select the one that has highest score. We follow the same flow for remaining types in pattern.
* Group POIs of type that match the pattern and then choose the ones that have shortes avg distance to other POIs. The basic idea is that if we choose the POIs that are closer to others then there's a chance that we may include higher number of POIs inside our solution.

Once we've chosen pivot POIs we cn sort remaining POIs in descending order based on their score and perform sequencial insertion to fill empty spaces without violating constraints.


## Simulated annealing
```
S <~ generate_initial_solutin()
Q <~ remaining_pois()
t <~ Set high temperature
Best <~ S
i <~ 0
repeat
    a <~ find_poi_highest_space(S) // Space ~> Travel time + duration
    for b in Q do
        R <~ swap(a, b)
        R <~ fill_empty(Q)
        if eval(R) > eval(S) or rand(0, 1) < e ** (eval(R) - eval(S)) / t then
            S <~ R
        end if
        
        if eval(S) > eval(Best) then
            Best <~ S
            i <~ 0
        else
            i <~ i + 1
        end if

        if i > MAX_ITER_WITHOUT_IMPROVEMENT then
            S, Q <~ shuffle(X, Y)
            S <~ fill_empty(Q)
        end if
    end for

    T <~ cooling_func()
until T = 0 
```

The movement operations that we perform here are:
* Swap ~ Replace one item inside solution with another one from outside of the solution
* Insert ~ Insert items from outside of the solution to fill empty gaps
* Shuffle ~ On each tour replace pivot POI with lowest score with other POIs that had lower score (see initial solution step), remove X elements from solution and swap Y elements between tours.

The cooling function is yet to be defined

## Running the program
Make sure you have installed dotnet core before running the app [https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial/install](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial/install)

In order to install the app you have to run `build.bat` or `build.sh` script based on your OS. If you're in windows you'll have to navigate to build directory to run the app (`.\\bin\\App\\netcoreapp2.2\\win10-x64\\MCTOPP.exe`), in other operating systems a shortcut is generated in current directory (`./program`).
