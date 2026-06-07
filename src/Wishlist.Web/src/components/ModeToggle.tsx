import { Monitor, Moon, Sun } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useTheme } from '@/context/useTheme'

export function ModeToggle() {
  const { mode, resolvedMode, setMode } = useTheme()

  const TriggerIcon =
    mode === 'system' ? Monitor : resolvedMode === 'dark' ? Moon : Sun

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Toggle theme mode">
          <TriggerIcon className="h-[1.1rem] w-[1.1rem]" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => setMode('light')}>
          <Sun className="mr-2 h-4 w-4" />
          Light
          {mode === 'light' && (
            <span className="ml-auto text-xs text-muted-foreground">●</span>
          )}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setMode('dark')}>
          <Moon className="mr-2 h-4 w-4" />
          Dark
          {mode === 'dark' && (
            <span className="ml-auto text-xs text-muted-foreground">●</span>
          )}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setMode('system')}>
          <Monitor className="mr-2 h-4 w-4" />
          System
          {mode === 'system' && (
            <span className="ml-auto text-xs text-muted-foreground">●</span>
          )}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
