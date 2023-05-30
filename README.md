# AtmosphericScattering

<div style="display: flex;">
    <img src="https://github.com/mharitsnf/AtmosphericScattering/assets/22760908/ebbddaef-94bb-45df-8748-959d279a0942" alt="Image 1" height="250" />
    <img src="https://github.com/mharitsnf/AtmosphericScattering/assets/22760908/79934c17-5ccb-44b6-ba87-144556244f95" alt="Image 2" height="250" />
</div>


In this project we set out to create a simulation of the atmosphere in terms of how light that is emitted by a light source (in our case the sun) is scattered when the light enters the atmosphere. The main problem was to generate a realistic atmosphere with the proper light conditions according to the algorithms and equations presented by previous research. Rayleigh and Mie scattering are known methods which we investigated and finally implemented Rayleigh scattering. For the implementation we wrote code in C++ using the OpenGL library. 

---

## Project Links
[Link to project blog](https://docs.google.com/document/d/1VtaNd2OQC4ndTJ4I_rilJaA5PS81mg9ZAMXCypCPn2M/edit?usp=sharing)

## How to run the project?
Firstly, go to branch `harits-dev` to get to the most updated development branch. There, On linux or other linux-based machines, run:
```
./configure.sh
./build.sh
./run.sh
```

## Notes
Make sure that in the external folder exists three folders: **glad**, **glew**, and **glfw**. If they don't exist, you need to run:
```
git submodule update --init --recursive
```
