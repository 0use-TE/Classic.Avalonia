import { dotnet } from './_framework/dotnet.js';

try {
    const runtime = await dotnet.withApplicationArgumentsFromQuery().create();
    await runtime.runMain(runtime.getConfig().mainAssemblyName, [globalThis.location.href]);
} catch (error) {
    console.error(error);
    document.getElementById('out').textContent = 'Unable to start Classic Demo. Please reload the page.';
}
