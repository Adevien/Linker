﻿using System.IO.Pipes;

namespace Linker
{
    static class LinkFactory
    {
        private static int _lastId;

        public static Link CreateConnection(PipeStream pipeStream, bool UseRecursion) => new Link(++_lastId, "Portal " + _lastId, pipeStream, UseRecursion);
    }
}
