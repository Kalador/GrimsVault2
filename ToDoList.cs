//TODO: Cache read/write inefficiency
//-------------------------------------------------------------------------------------------------
//You re-read the cache on every update:
//PowerShellvar cache = vault.Cache.ReadCache<Dictionary<string, CexBox>>(CacheFile)Show more lines
//inside UpdateCache
//If RefreshRollingAsync runs through 6 items, you’ll:
//read cache 6 times
//write cache 6 times
//Not catastrophic, but avoidable.
//Suggestion:
//Load once → mutate in memory → write once
//or
//Introduce a simple cache wrapper
//-------------------------------------------------------------------------------------------------


//TODO: Race condition potential (minor but real)
//-------------------------------------------------------------------------------------------------
//Multiple refreshes or requests could:
//read cache simultaneously
//overwrite each other
//If this ever runs in parallel (cron + user-triggered retry), you might lose updates.
//Not urgent, but worth noting.
//-------------------------------------------------------------------------------------------------


//TODO: Slight duplication in formatting logic
//-------------------------------------------------------------------------------------------------
//You’ve got:
//FormatPrice(local)
//box.SellPrice.FormatPrice()(extension ?)
//FormatAge
//box.LastRefreshed.FormatAge()
//Feels a bit split-brain.
//Not a bug — just inconsistent ownership of formatting.
//-------------------------------------------------------------------------------------------------


//TODO: Minor inefficiency in BuildTicker
//-------------------------------------------------------------------------------------------------
//You do multiple passes over StoresInStock:
//once for priority
//once for counts
//Totally fine at this scale, just noting it.
//-------------------------------------------------------------------------------------------------