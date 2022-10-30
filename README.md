# Custom Skinning For Unity
<img src="Assets/Project/animations/gifs/demo.gif" width="500" />

## Intro
The main purpose of this project was to work towards implementing the optimized centers of rotation skinning proposed by [this](https://la.disneyresearch.com/publication/skinning-with-optimized-cors/) paper.
This project includes custom implementations of linear blend skinning (LBS), dual quaternion skinning (DQS), and optimized centers of rotation skinning (Opt. CoR).

## Performance
There are two aspects of performance relavent for this code: pre-processing and real-time.
### Pre-processing times
For pre-processing, the bone weights, bone ids, and optimized CoRs are baked into the UVs of the mesh. The runtime of this preprocessing depends on the size of the mesh and number of bones. However, with utilizing a Compute Shader for the compute-heavy task of finding the optimized CoRs for each vertex, the entire baking process takes fractions of a second. During my testing, given my PC, the baking for a fully rigged character took on average 300-350 miliseconds.
### Real-time usage and bottleneck
In a realistic setting, meaning a realistic number of characters with close or medium distance LODs, this alternate skinning is perfectly viable. For example, even with hundreds of characters using Opt. CoR skinning, the game still runs at hundreds of fps. However, compared to the built-in skinning, there certainly is a drop in performance. This is due to a bottleneck created from needing to transfer the bone transform data from the CPU to the GPU every frame. For a future implementation, I would like to explore the use of ECS to alleviate this bottleneck.