# trgen-unity
A Unity Package used to communicate with TriggerBox


## Getting started

To install this package add this package inside the `Packages/manifest.json` file

```json
{
  "dependencies": {
    ...
    ...
    "com.stefanolatini.trgen": "0.0.1",
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