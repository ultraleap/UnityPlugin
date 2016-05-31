The FramerateGraph is a simple asset that aims to provide basic runtime information about frame timing.  

Graph:
The graph contains an upper and a lower graph, which have different meanings.  The upper graph shows the
time spent in script execution such as FixedUpdate / Update callbacks.  The lower graph shows the time 
spent doing rendering for all cameras.  

Right Panel:
The right panel has three numbers, a top, middle, and bottom.
The top and bottom numbers correspond with the top and bottom graphs, and represent the average time in
milliseconds spent on the update cycle or the render cycle respectively.
The middle number shows the average time in milliseconds spent on the entire frame, rendering and update
indluded.

Top Panel:
The top panel shows the frequency of 'spikes' in seconds.  A spike is defined a a frame that takes significantly
longer to generate than expected.  This can provide useful information for troubleshooting things like garbage
collection.