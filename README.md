<p align="center">
  <img src="images/banner.png" alt="TriggerBox Banner" width="600px" height="300px">
</p>
<h1 align="center">TriggerBox CLI</h1>
![Version: 1.0.12](https://img.shields.io/badge/Version-1.0.12informational?style=flat-square) ![Type: application](https://img.shields.io/badge/Type-application-informational?style=flat-square) ![AppVersion: 1.0.12](https://img.shields.io/badge/AppVersion-1.0.12-informational?style=flat-square)
<h3 align="center"> A Unity library that manages Ethernet socket communication with the CoSANLab TriggerBox device </h3>


## Getting started

To install this package add this package inside the `Packages/manifest.json` file

```json
{
  "dependencies": {
    ...
    ...
    "com.cosanlab.trgen": "1.0.0",
  }
```

## Example


```cs
using UnityEngine;
using System.Collections.Generic;

namespace Trgen.Demo
{
    public class TriggerBoxExample : MonoBehaviour
    {
        private TriggerClient client;

        void Start()
        {
            // Default IP "192.168.123.1"
            // Default Port = 4242
            client = new TriggerClient();

            // Check availability
            Debug.Log("TriggerBox available: " + client.IsAvailable());

            // Example: start single Trigger Pin
            client.StartTrigger(TriggerPin.NS3);

            // Example 2: start multiple Trigger Pins
            var pins = new List<int> { TriggerPin.GPIO0, TriggerPin.SA2 };
            client.StartTriggerList(pins);

            // Read state
            int level = client.GetLevel();
            Debug.Log("Level: " + level);
        }
    }
}

```