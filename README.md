# Distributed Actor Mining

<b>Course</b>: COP5615 - Distributed Operating System Principles <br>
<b>Institute</b>: Unviersity of Florida <br>
<b>Semester</b>: Fall 2021 <br>
<b>Instructor</b>: Dr. Alin Dobra <br>
<b>Team</b>: 
* Prateek Kumar Goel ([Github](https://github.com/pkgprateek))
* Malvika Ranjitsinh Jadhav ([Github](https://github.com/malvikajadhav))


## Problem definition
Bitcoins (seehttp://en.wikipedia.org/wiki/Bitcoin) are the most popular crypto-currency in common use. At their heart, bitcoins use the hardness of cryptographic hashing (for a reference seehttp://en.wikipedia.org/wiki/Cryptographichashfunction)to ensure a limited “supply” of coins.  In particular, the key component in a bit-coin is an input that, when “hashed” produces an output smaller than a target value.  In practice, the comparison values have leading  0’s, thus the bitcoin is required to have a given number of leading 0’s (to ensure 3 leading 0’s, you look for hashes smaller than0x001000... or smaller or equal to 0x000ff....The hash you are required to use is SHA-256.  You can check your version against this online hasher:http://www.xorbin.com/tools/sha256-hash-calculator. For example, when the text “COP5615 is a boring class” is hashed, the value 0xe9a425077e7b492076b5f32f58d5eb6824b1875621e6237f1a2430c6b77e467c is obtained.  For the coins, you find, check your answer with this calculator to ensure correctness. The goal of this first project is to use F# and the actor model to build a good solution to this problem that runs well on multi-core machines.

## Requirements

### Input: 
The input provided (as command line to yourproject1.fsx) will be, the required number of 0’s of the bitcoin.
```console
foo@bar~$ dotnet fsi server.fsx 2
```


### Output: 
Print, on independent entry lines, the input string, and the correspondingSHA256 hash separated by a TAB, for each of the bitcoins you find. Obviously, your SHA256 hash must have the required number of leading 0s (k= 3 means3 0’s in the hash notation).  An extra requirement, to ensure every group finds different coins, is to have the input string prefixed by the gator link ID of one of the team members.

<b>Example 1:</b>
```console
foo@bar~$ dotnet fsi main.fsx 2
```
```console
foo@bar~$ prateekgoel;kjsdfk11 00402337f95d018438aad6c7dd75ad6e9239d6060444a7a6b26299b261aa9a8b
```
indicates that the coin with 1 leading 0 is adobra;kjsdfk11and it is prefixed by the gatorlink ID adobra.

 

## Distributed implementation
The more cores you have to more coins you can mine.  To this end, enlisting other machines adds to your coin mining capabilities.  Extendproject1.fsx so that the argument is a computer address or IP address of the server.  This program then becomes a “worker” and contacts the server to get work.  This second program will not display anything.  All the coins found have to be displayed by the server.

<b>Example 2:</b>
``` console
foo@bar~$ dotnet fsi client.fsx 10.22.13.155
```
will start a  worker that contacts the F# server hosted at  10.22.13.155  and participates in mining.   Hint.   when testing this,  have your project partner start a server, find the IP address of the server and then start the worker. Notice,  your server should be able to mine coins without any workers but has to accommodate workers as they become available.

## Actor modeling
In this project, you have to use exclusively the AKKA actor library in F# (projects that do not use multiple actors or use any other form of parallelism will receive no credit).  A model similar to the one indicated in class for the problem of adding up a lot of numbers can be used here,  in particular, define worker actors that are given a range of problems to solve and boss that keeps track of all the problems and perform the job assignment.

<br>

<b>Model Implementation: </b>
This is a remote actor model which involves two participating systems. 
Server: 
- It accepts user input, which is number of leading zeros for the bitcoin we want to mine.
- Listens to any workers that message the server when available for work


<b>Work unit:</b>
In a single request from the boss actor our each worker actor works on  a set of 5 tasks. Considering this set of tasks as a work unit we have found the following values for CPU time to Real time ratio :

| Number of work units | CPU Time (ms) | Real Time (ms) |  Ratio |
|----------------------|---------------|----------------|--------|
|          100         | 1656          | 493            | 3.36   |
|          500         | 5843          | 1059           | 5.52   |
|         10000        | 23406         | 2791           | 8.39   |
|        100000        | 30703         | 3768           | 8.14   |
|       1000000        | 34484         | 4278           | 8.06   |

Thus our system had its optimum performance with 10000 work units.

<b>Running time : </b><br>
To find the first bitcoin with 4 leading zeros -<br>
CPU Time: 1656ms<br>
Real Time: 493ms<br>
Ratio: 3.36 <br>

<b> CPU TIME and Utilization:</b>
We have calculated the values for the following number of workers when number of work units assigned was 500:
| Number of workers | CPU Time (ms) | Real Time (ms) |  Ratio |
|-------------------|---------------|----------------|--------|
|         12        | 1656          | 1059           | 5.52   |
|         24        | 19250         | 2769           | 6.95   |
|         120       | 44015         | 6958           | 6.325  |
|         600       | 187078        | 30644          | 6.12   |

